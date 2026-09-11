using UnityEngine;

// Shared fit target for CameraAspectFit. designWidth/designLength are world
// units describing the reference "screenful" of gameplay this game mode was
// authored at — designLength is the vertical extent (world height),
// designWidth the horizontal extent. Their ratio is the target aspect: above
// it the camera fits to length, at or below it the camera fits to width. All
// scenes of a given mode (e.g. every gameplay scene) share the same asset so
// the fit behaves identically everywhere.
//
// The target device is the iPhone 15 (1179x2556, aspect 0.4613): every
// profile keeps that ratio, only the zoom (absolute size) differs per mode.
[CreateAssetMenu(fileName = "ScreenFitProfile", menuName = "Game/Screen Fit Profile")]
public class ScreenFitProfile : ScriptableObject
{
    public float designWidth = 7.875f;
    public float designLength = 7.875f * 2556f / 1179f;
}
