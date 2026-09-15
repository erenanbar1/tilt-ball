using PrimeTween;
using UnityEngine;

// Level 1's teacher. Lives in that level's layout prefab, so it comes and goes
// with the level and the Gameplay scene never knows about it. Three beats, each
// dismissed by the player actually doing the thing rather than by a timer:
//   1. LIFT  — chevrons over both stick ends: hold both buttons and the bar rises.
//   2. TILT  — a ghost of the stick above the real one tips toward the hole and
//              a ghost ball rolls down it, while a chevron marks the button to
//              hold; done once the player has tilted the bar themselves.
//   3. AIM   — a runway of dots from the ball to the winning hole.
// Every visual is built at runtime from the real stick's and ball's own sprites,
// so the ghost always matches whatever the rig looks like.
public class TiltTutorial : MonoBehaviour
{
    [Header("Art")]
    public Sprite chevronSprite;   // points +x at rest
    public Sprite dotSprite;

    [Header("Thresholds")]
    public float liftDone = 1.5f;          // stick centre rise, world units
    public float tiltDone = 10f;           // degrees of tilt the player must reach
    public float aimDone = 1.2f;           // distance to the hole that ends the runway

    [Header("Look")]
    public Color hintColor = new Color(1f, 0.93f, 0.6f, 1f);
    public float ghostAlpha = 0.38f;
    public int sortingOrder = 12;
    public float textSize = 0.34f;         // world height of a capital letter, roughly
    public float ghostHeight = 2.2f;       // how far above the real stick the ghost floats

    enum Step { Lift, Tilt, Aim, Done }

    StickController stick;
    BallOnPlatformController ball;
    Collider2D ballCollider;
    Transform hole;
    float stickBaseY;
    float ghostSide = 1f;                  // +1: the hole is to the right of the ball

    Step step = Step.Lift;
    Transform group;
    TextMesh label;
    Transform chevronLeft, chevronRight, chevronTilt;
    Transform ghostStick, ghostBall;
    SpriteRenderer[] runway;
    Sequence ghostLoop;

    void Start()
    {
        stick = FindFirstObjectByType<StickController>();
        ball = FindFirstObjectByType<BallOnPlatformController>();
        var win = FindFirstObjectByType<WinTrigger>();
        if (stick == null || ball == null || win == null)
        {
            enabled = false;
            return;
        }
        hole = win.transform;
        ballCollider = ball.GetComponent<Collider2D>();
        stickBaseY = stick.transform.position.y;

        group = new GameObject("TutorialHints").transform;
        group.SetParent(transform, false);

        label = BuildLabel();
        chevronLeft = BuildChevron("ChevronLeft");
        chevronRight = BuildChevron("ChevronRight");
        chevronTilt = BuildChevron("ChevronTilt");
        BuildGhost();
        runway = BuildRunway(7);

        EnterLift();
    }

    void OnDestroy()
    {
        ghostLoop.Stop();
    }

    void LateUpdate()
    {
        if (step == Step.Done || stick == null) return;

        // The hole took the ball: nothing left to teach.
        if (!ballCollider.enabled)
        {
            Finish();
            return;
        }

        switch (step)
        {
            case Step.Lift:
                FollowStickEnds();
                if (stick.transform.position.y - stickBaseY > liftDone) EnterTilt();
                break;
            case Step.Tilt:
                FollowGhost();
                if (Mathf.Abs(Mathf.DeltaAngle(0f, stick.transform.eulerAngles.z)) > tiltDone) EnterAim();
                break;
            case Step.Aim:
                LayRunway();
                if (Vector2.Distance(ball.transform.position, hole.position) < aimDone) Finish();
                break;
        }
    }

    // ---- Step 1: lift -------------------------------------------------------

    void EnterLift()
    {
        step = Step.Lift;
        SetText("HOLD BOTH SIDES\nTO LIFT THE BAR");
        Show(chevronLeft);
        Show(chevronRight);
    }

    void FollowStickEnds()
    {
        StickEnds(out Vector3 left, out Vector3 right);
        chevronLeft.position = left + Vector3.up * 0.9f;
        chevronRight.position = right + Vector3.up * 0.9f;
        label.transform.position = new Vector3(0f, stick.transform.position.y + 2.6f, 0f);
    }

    // ---- Step 2: tilt -------------------------------------------------------

    void EnterTilt()
    {
        step = Step.Tilt;
        Hide(chevronLeft);
        Hide(chevronRight);

        // The bar has to dip toward the hole, which means raising the *other*
        // end — that is the button the chevron points at.
        ghostSide = hole.position.x > ball.transform.position.x ? 1f : -1f;
        SetText("HOLD ONE SIDE TO TILT\nTHE BALL ROLLS DOWNHILL");
        Show(chevronTilt);
        ghostStick.gameObject.SetActive(true);
        PlayGhostLoop();
    }

    void FollowGhost()
    {
        // Rides above the real stick, but never up into the hole or the HUD's
        // top bar if the player keeps lifting without tilting.
        Vector3 basePos = stick.transform.position + Vector3.up * ghostHeight;
        basePos.y = Mathf.Min(basePos.y, hole.position.y - 1.6f);
        ghostStick.position = basePos;
        StickEnds(out Vector3 left, out Vector3 right);
        chevronTilt.position = (ghostSide > 0f ? left : right) + Vector3.up * 0.9f;
        label.transform.position = new Vector3(0f, basePos.y + 2.0f, 0f);
    }

    void PlayGhostLoop()
    {
        ghostLoop.Stop();
        float tilt = -ghostSide * 16f;                              // negative z = right end down
        float travel = ghostSide * 2.6f / Mathf.Max(ghostStick.localScale.x, 1e-4f);
        var stickSr = ghostStick.GetComponent<SpriteRenderer>();
        var ballSr = ghostBall.GetComponent<SpriteRenderer>();
        var level = Quaternion.identity;
        var tipped = Quaternion.Euler(0f, 0f, tilt);
        ghostBall.localPosition = new Vector3(0f, GhostBallLift(), 0f);

        ghostLoop = Sequence.Create(cycles: -1)
            .Group(Tween.Alpha(stickSr, 0f, ghostAlpha, 0.25f))
            .Group(Tween.Alpha(ballSr, 0f, ghostAlpha + 0.25f, 0.25f))
            .Chain(Tween.LocalRotation(ghostStick, level, tipped, 0.7f, Ease.InOutSine))
            .Group(Tween.LocalPositionX(ghostBall, 0f, travel, 1.0f, Ease.InQuad, startDelay: 0.25f))
            .ChainDelay(0.5f)
            .Chain(Tween.Alpha(stickSr, ghostAlpha, 0f, 0.3f))
            .Group(Tween.Alpha(ballSr, ghostAlpha + 0.25f, 0f, 0.3f))
            .ChainDelay(0.2f);
    }

    // ---- Step 3: aim --------------------------------------------------------

    void EnterAim()
    {
        step = Step.Aim;
        ghostLoop.Stop();
        ghostStick.gameObject.SetActive(false);
        Hide(chevronTilt);
        SetText("GUIDE THE BALL\nINTO THE HOLE");
        for (int i = 0; i < runway.Length; i++)
        {
            var sr = runway[i];
            sr.gameObject.SetActive(true);
            Tween.Alpha(sr, 0.25f, 0.9f, 0.6f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, startDelay: i * 0.09f);
        }
    }

    void LayRunway()
    {
        Vector3 from = ball.transform.position;
        Vector3 to = hole.position;
        for (int i = 0; i < runway.Length; i++)
        {
            float t = (i + 1f) / (runway.Length + 1f);
            runway[i].transform.position = Vector3.Lerp(from, to, t);
        }
        label.transform.position = new Vector3(0f, to.y + 1.6f, 0f);
    }

    void Finish()
    {
        step = Step.Done;
        ghostLoop.Stop();
        foreach (var sr in group.GetComponentsInChildren<SpriteRenderer>())
        {
            Tween.StopAll(sr);
            Tween.Alpha(sr, 0f, 0.3f);
        }
        Tween.Custom(this, 1f, 0f, 0.3f, (t, a) => t.SetLabelAlpha(a))
            .OnComplete(this, t => { if (t.group != null) t.group.gameObject.SetActive(false); });
    }

    // ---- builders -----------------------------------------------------------

    TextMesh BuildLabel()
    {
        var go = new GameObject("Label");
        go.transform.SetParent(group, false);
        var tm = go.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 64;
        tm.fontStyle = FontStyle.Bold;
        tm.characterSize = textSize / 6.4f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = hintColor;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = tm.font.material;
        mr.sortingOrder = sortingOrder;
        return tm;
    }

    Transform BuildChevron(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(group, false);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);   // +x art turned to point up
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = chevronSprite;
        sr.color = hintColor;
        sr.sortingOrder = sortingOrder;
        go.SetActive(false);
        return go.transform;
    }

    void BuildGhost()
    {
        var realStick = stick.GetComponent<SpriteRenderer>();
        var realBall = ball.GetComponent<SpriteRenderer>();

        var s = new GameObject("GhostStick");
        s.transform.SetParent(group, false);
        var ssr = s.AddComponent<SpriteRenderer>();
        if (realStick != null)
        {
            ssr.sprite = realStick.sprite;
            ssr.drawMode = realStick.drawMode;
            if (realStick.drawMode != SpriteDrawMode.Simple) ssr.size = realStick.size;
            s.transform.localScale = realStick.transform.lossyScale;
        }
        ssr.sortingOrder = sortingOrder - 1;
        ssr.color = WithAlpha(Color.white, 0f);
        ghostStick = s.transform;

        var b = new GameObject("GhostBall");
        b.transform.SetParent(ghostStick, false);
        var bsr = b.AddComponent<SpriteRenderer>();
        if (realBall != null) bsr.sprite = realBall.sprite;
        // The ghost stick carries the real stick's scale, so the ball's own scale
        // has to be expressed inside that for the ghost ball to come out true size.
        Vector3 ls = ghostStick.localScale;
        Vector3 bs = realBall != null ? realBall.transform.lossyScale : Vector3.one * 0.05f;
        b.transform.localScale = new Vector3(bs.x / Mathf.Max(ls.x, 1e-4f), bs.y / Mathf.Max(ls.y, 1e-4f), 1f);
        bsr.sortingOrder = sortingOrder;
        bsr.color = WithAlpha(Color.white, 0f);
        ghostBall = b.transform;
        ghostBall.localPosition = new Vector3(0f, GhostBallLift(), 0f);

        s.SetActive(false);
    }

    // The ghost ball's height above the ghost stick's centre, in the ghost
    // stick's local units — the same gap the real ball keeps.
    float GhostBallLift()
    {
        float worldGap = ball.transform.position.y - stick.transform.position.y;
        return worldGap / Mathf.Max(ghostStick.localScale.y, 1e-4f);
    }

    SpriteRenderer[] BuildRunway(int count)
    {
        var dots = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("RunwayDot" + i);
            go.transform.SetParent(group, false);
            go.transform.localScale = Vector3.one * 0.28f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.color = WithAlpha(hintColor, 0f);
            sr.sortingOrder = sortingOrder;
            go.SetActive(false);
            dots[i] = sr;
        }
        return dots;
    }

    // ---- helpers ------------------------------------------------------------

    void StickEnds(out Vector3 left, out Vector3 right)
    {
        Vector3 along = stick.transform.right * stick.stickHalfWidth;
        left = stick.transform.position - along;
        right = stick.transform.position + along;
    }

    void SetText(string text)
    {
        label.text = text;
        SetLabelAlpha(0f);
        Tween.Custom(this, 0f, 1f, 0.35f, (t, a) => t.SetLabelAlpha(a));
    }

    void SetLabelAlpha(float a)
    {
        if (label != null) label.color = WithAlpha(hintColor, a);
    }

    // Chevrons pulse in brightness and size so they read as "press here".
    void Show(Transform chevron)
    {
        chevron.gameObject.SetActive(true);
        var sr = chevron.GetComponent<SpriteRenderer>();
        Tween.Alpha(sr, 0.35f, 1f, 0.45f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
        Tween.LocalScale(chevron, Vector3.one * 1.1f, Vector3.one * 1.45f, 0.45f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo);
    }

    void Hide(Transform chevron)
    {
        Tween.StopAll(chevron);
        Tween.StopAll(chevron.GetComponent<SpriteRenderer>());
        chevron.gameObject.SetActive(false);
    }

    static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
}
