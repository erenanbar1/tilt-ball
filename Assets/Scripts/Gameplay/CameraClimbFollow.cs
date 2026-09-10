using UnityEngine;

// Scrolls the gameplay camera vertically so a tall level's climb stays in frame.
// Classic levels are shorter than one screenful, and ConfigureBounds collapses
// their travel range to a single point, so this component can sit on a Classic
// camera without moving it at all.
[RequireComponent(typeof(Camera))]
public class CameraClimbFollow : MonoBehaviour
{
    // The stick, not the ball. The stick's centre rises smoothly at the rig's own
    // speed, while the ball's height swings as it rolls along the tilt — following
    // that would shake the camera for reasons the player didn't cause.
    public Transform target;

    // Sits the camera slightly above the stick, so the climb ahead gets more of
    // the frame than the ground already covered.
    public float followOffsetY = 2.5f;
    public float smoothTime = 0.18f;

    [Header("Travel range — derived from the level's bounds")]
    public float minY;
    public float maxY;

    Camera cam;
    float velocityY;

    // The level's own extents, kept so the travel range can be worked out again
    // whenever the camera's size changes underneath it.
    float floorY;
    float ceilingY;
    bool hasBounds;
    float boundsOrthographicSize = -1f;

    // Lazy rather than cached in Awake: LevelController configures this from its
    // own Awake, and Unity doesn't order Awake between objects.
    Camera Cam => cam != null ? cam : (cam = GetComponent<Camera>());

    // floorY/ceilingY are the level's world bounds. The camera's centre is kept
    // half a view inside them so neither edge is ever on screen; a level too short
    // to allow that simply centres, which is every Classic level.
    public void ConfigureBounds(float floorY, float ceilingY)
    {
        this.floorY = floorY;
        this.ceilingY = ceilingY;
        hasBounds = true;
        RecomputeTravel();
    }

    void RecomputeTravel()
    {
        float half = Cam.orthographicSize;
        boundsOrthographicSize = half;
        minY = floorY + half;
        maxY = ceilingY - half;
        if (maxY < minY) minY = maxY = (floorY + ceilingY) * 0.5f;
    }

    // How far the camera can actually see, top and bottom, across its whole
    // travel. On a level long enough to scroll this is exactly the level's own
    // bounds; on a short one, where the camera parks in the middle, the view can
    // reach past them — which is what the background has to be built to cover.
    public void GetVisibleRange(out float bottom, out float top)
    {
        float half = Cam.orthographicSize;
        bottom = minY - half;
        top = maxY + half;
    }

    // Without this the camera would glide in from wherever the scene left it on
    // the first frame of a level.
    public void SnapToTarget()
    {
        if (target == null) return;
        velocityY = 0f;
        Vector3 pos = transform.position;
        pos.y = DesiredY();
        transform.position = pos;
    }

    void LateUpdate()
    {
        // CameraAspectFit grows orthographicSize on a narrow screen and reapplies
        // it whenever the screen changes — a phone rotating, the Game view being
        // resized, a different device picked in the Simulator. Half a view is
        // exactly what the travel range is inset by, so a size that moved has to
        // be worked back through rather than left at whatever it was on the first
        // frame, or the camera stops short of the summit or scrolls past the
        // bottom of the level.
        if (hasBounds && !Mathf.Approximately(Cam.orthographicSize, boundsOrthographicSize)) RecomputeTravel();

        if (target == null) return;

        Vector3 pos = transform.position;
        // Unscaled: the pause menu sets Time.timeScale to 0, and a glide left
        // half-finished there would lurch the moment the player resumed.
        pos.y = Mathf.SmoothDamp(pos.y, DesiredY(), ref velocityY, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        transform.position = pos;
    }

    float DesiredY() => Mathf.Clamp(target.position.y + followOffsetY, minY, maxY);
}
