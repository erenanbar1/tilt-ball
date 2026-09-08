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

    [Header("Travel range — set by LevelController")]
    public float minY;
    public float maxY;

    Camera cam;
    float velocityY;

    // Lazy rather than cached in Awake: LevelController configures this from its
    // own Awake, and Unity doesn't order Awake between objects.
    Camera Cam => cam != null ? cam : (cam = GetComponent<Camera>());

    // floorY/ceilingY are the level's world bounds. The camera's centre is kept
    // half a view inside them so neither edge is ever on screen; a level too short
    // to allow that simply centres, which is every Classic level.
    public void ConfigureBounds(float floorY, float ceilingY)
    {
        float half = Cam.orthographicSize;
        minY = floorY + half;
        maxY = ceilingY - half;
        if (maxY < minY) minY = maxY = (floorY + ceilingY) * 0.5f;
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
        if (target == null) return;

        Vector3 pos = transform.position;
        // Unscaled: the pause menu sets Time.timeScale to 0, and a glide left
        // half-finished there would lurch the moment the player resumed.
        pos.y = Mathf.SmoothDamp(pos.y, DesiredY(), ref velocityY, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        transform.position = pos;
    }

    float DesiredY() => Mathf.Clamp(target.position.y + followOffsetY, minY, maxY);
}
