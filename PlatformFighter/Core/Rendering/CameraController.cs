using Godot;
using PlatformFighter.Core.Math;
using PlatformFighter.Core.Sim.Collision;

namespace PlatformFighter.Core.Rendering;

/// <summary>
/// Melee-style framing camera (Master Directive v3, Camera Development
/// Directive). Faithfully ports real Melee's camera ALGORITHM — subjects →
/// bounds → interest → smoothed position — as documented in the decompiled
/// camera code (walz0/MeleeCameraUE5): the camera builds a bounding box
/// around all subjects (the fighters), derives an "interest" point it looks
/// at, clamps that against the stage's per-stage camera limit box
/// (StageGeometry.CameraBounds), and smoothly interpolates toward it so
/// pan/zoom are never instantaneous.
///
/// WHAT IS AND ISN'T PORTED: MeleeLight has no camera at all, so there is no
/// MeleeLight camera to port — this deliberately targets REAL Melee instead,
/// exactly as v3 directs. The structure (subject bounds, interest, per-stage
/// limit clamp, smoothing) is real-Melee-accurate. The scalar tuning values
/// (margins, zoom range, smoothing rate, vertical bias) are authored for THIS
/// engine's sim units and orthographic 2D setup — Melee's own numbers are 3D
/// perspective units that don't transfer — and are flagged as the intended
/// tuning point for a later footage-comparison pass. They are NOT presented
/// as frame-exact Melee constants (Directive Rule: never pass invented values
/// off as source-of-truth).
///
/// COORDINATES: operates entirely in sim units on input (fighter positions,
/// stage CameraBounds) and produces a world-node Scale (zoom) and Position
/// (pan) for Main._world — i.e. this is RENDER layer, gameplay-authoritative
/// rendering-adapts (v3 coordinate rule). It reads sim state; it never writes
/// it, so it stays out of the deterministic sim and needs no SaveState.
/// </summary>
public sealed class CameraController
{
    // ---- Tuning values (this engine's sim units — NOT ported Melee constants) ----

    /// <summary>Extra sim-unit padding added around the fighters' bounding box
    /// so nobody is framed flush against the screen edge.</summary>
    private const float MarginX = 30f;
    private const float MarginY = 26f;

    /// <summary>Zoom = viewport / framed-width, but clamped: never zoom in
    /// closer than MinZoom (two fighters standing on top of each other would
    /// otherwise fill the screen) nor out past MaxZoom (keeps pixels legible).
    /// MaxZoom matches Main's old fixed RenderScale (4.5) so the default framing
    /// on spawn reads like the pre-camera view.</summary>
    private const float MinZoom = 2.2f;
    private const float MaxZoom = 4.5f;

    /// <summary>Interest sits slightly ABOVE the geometric midpoint — Melee
    /// biases the framing upward so aerial play has headroom. Sim units,
    /// screen-up (this codebase is +Y-down, so subtracted).</summary>
    private const float VerticalBias = 12f;

    /// <summary>Per-second exponential smoothing rate for pan and zoom. Higher =
    /// snappier. Frame-rate-independent via the delta-based lerp below, so it
    /// behaves the same at 60 and 144 fps (view-side only — the sim is fixed
    /// step regardless).</summary>
    private const float PanSmoothing = 8f;
    private const float ZoomSmoothing = 6f;

    private readonly Vector2 _viewportSize;
    private readonly StageGeometry _stage;

    private Vector2 _interest;   // sim-space point the camera centers on
    private float _zoom;         // current sim->screen scale
    private bool _initialized;

    public CameraController(StageGeometry stage, Vector2 viewportSize)
    {
        _stage = stage;
        _viewportSize = viewportSize;
        _zoom = MaxZoom;
    }

    /// <summary>Call every render frame (from Main._Process) with the two
    /// fighters' interpolated render-space sim positions and the frame delta.
    /// Returns the Scale and Position to apply to Main._world.</summary>
    public (Vector2 scale, Vector2 position) Update(Vector2 p1, Vector2 p2, float delta)
    {
        // ---- Subjects -> bounds: AABB around both fighters, plus margin. ----
        float minX = Mathf.Min(p1.X, p2.X) - MarginX;
        float maxX = Mathf.Max(p1.X, p2.X) + MarginX;
        float minY = Mathf.Min(p1.Y, p2.Y) - MarginY;
        float maxY = Mathf.Max(p1.Y, p2.Y) + MarginY;

        // ---- Clamp bounds to the stage camera limit box (real-Melee per-stage
        // limit). Falls back to blast zone, then to unclamped, so a stage with
        // no camera data still frames sanely instead of throwing. ----
        if (_stage.CameraBounds.HasValue)
        {
            var cb = _stage.CameraBounds.Value;
            float limMinX = cb.Center.X.ToFloat() - cb.HalfSize.X.ToFloat();
            float limMaxX = cb.Center.X.ToFloat() + cb.HalfSize.X.ToFloat();
            float limMinY = cb.Center.Y.ToFloat() - cb.HalfSize.Y.ToFloat();
            float limMaxY = cb.Center.Y.ToFloat() + cb.HalfSize.Y.ToFloat();
            minX = Mathf.Max(minX, limMinX); maxX = Mathf.Min(maxX, limMaxX);
            minY = Mathf.Max(minY, limMinY); maxY = Mathf.Min(maxY, limMaxY);
        }

        // ---- Interest: bounds center, biased upward. ----
        var targetInterest = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f - VerticalBias);

        // ---- Zoom: fit framed box in viewport, take the tighter axis, clamp. ----
        float framedW = Mathf.Max(maxX - minX, 1f);
        float framedH = Mathf.Max(maxY - minY, 1f);
        float zoomX = _viewportSize.X / framedW;
        float zoomY = _viewportSize.Y / framedH;
        float targetZoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), MinZoom, MaxZoom);

        // ---- Smooth (frame-rate-independent exponential approach). First frame
        // snaps so the match doesn't open mid-pan. ----
        if (!_initialized)
        {
            _interest = targetInterest;
            _zoom = targetZoom;
            _initialized = true;
        }
        else
        {
            _interest = _interest.Lerp(targetInterest, 1f - Mathf.Exp(-PanSmoothing * delta));
            _zoom = Mathf.Lerp(_zoom, targetZoom, 1f - Mathf.Exp(-ZoomSmoothing * delta));
        }

        // ---- World transform: scale by zoom, then position so _interest lands
        // at viewport center. world.Position = viewportCenter - interest*zoom. ----
        var scale = new Vector2(_zoom, _zoom);
        var position = _viewportSize * 0.5f - _interest * _zoom;
        return (scale, position);
    }
}
