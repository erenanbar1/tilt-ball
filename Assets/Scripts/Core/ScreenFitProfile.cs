using UnityEngine;

// Shared fit target for CameraAspectFit. designWidth/designLength are world
// units describing the reference "screenful" of gameplay this game mode was
// authored at — designLength is the vertical extent (world height),
// designWidth the horizontal extent. Their ratio is the target aspect: above
// it the camera fits to length, at or below it the camera fits to width. All
// scenes of a given mode (e.g. every gameplay scene) share the same asset so
// the fit behaves identically everywhere.
[CreateAssetMenu(fileName = "ScreenFitProfile", menuName = "Game/Screen Fit Profile")]
public class ScreenFitProfile : ScriptableObject
{
    public float designWidth = 7.875f;
    public float designLength = 14f;
}
