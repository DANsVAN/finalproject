using Godot;

public partial class CameraController : Camera2D
{
	#region Zoom Settings
	[ExportGroup("Zoom")]
	[Export(PropertyHint.Range, "0.01,10.0,0.01")] private float minZoom = 0.06f;
	[Export(PropertyHint.Range, "0.1,10.0,0.01")] private float maxZoom = 1.2f;
	[Export(PropertyHint.Range, "0.01,1.0,0.01")] private float zoomStep = 0.1f;
	[Export(PropertyHint.Range, "0.0,30.0,0.1")] private float zoomSmoothing = 12.0f;
	[Export(PropertyHint.Range, "0.5,30.0,0.1")] private float keyboardZoomStepsPerSecond = 5.0f;
	#endregion

	#region Pan Settings
	[ExportGroup("Pan")]
	[Export] private bool keyboardPanEnabled = true;
	[Export] private bool mouseDragEnabled = true;
	// Viewport (screen) space speed: world pan uses panSpeed / Zoom so zoomed-out views
	// (small Zoom in Godot) pan faster in world units and feel consistent.
	[Export(PropertyHint.Range, "50,5000,10")] private float panSpeed = 4000.0f;
	[Export(PropertyHint.Range, "0.1,5.0,0.1")] private float dragPanSensitivity = 3f;
	[Export(PropertyHint.Range, "0.0,30.0,0.1")] private float positionSmoothing = 14.0f;
	#endregion

	#region Bounds Settings
	[ExportGroup("Bounds")]
	[Export] private bool enforceBounds = true;
	[Export] private Rect2 worldBounds = new Rect2(0, 0, 800, 400);
	[Export(PropertyHint.Range, "0,2000,1")] private float boundsExpansion = 24.0f;
	[Export(PropertyHint.Range, "0,5000,1")] private float boundsPadding = 0.0f;
	#endregion

	private float _targetZoomScalar = 1.0f;
	private Vector2 _targetPosition;
	private bool _isMiddleMouseDragging;

	public override void _Ready()
	{
		_targetZoomScalar = Zoom.X;
		_targetPosition = GlobalPosition;
		MakeCurrent();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		HandleZoomInput(@event);
		HandleKeyboardZoomEvent(@event);
		HandleMiddleMouseState(@event);

		if (!mouseDragEnabled || !_isMiddleMouseDragging)
			return;

		if (@event is InputEventMouseMotion mouseMotion)
		{
			// Relative is in viewport pixels; divide by Zoom to convert to world movement
			// (same convention as keyboard pan — small Zoom = zoomed out = larger world delta).
			float z = Mathf.Max(Zoom.X, 0.001f);
			_targetPosition -= mouseMotion.Relative * dragPanSensitivity / z;
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		HandleKeyboardZoomHold(dt);
		HandlePanInput(dt);
		UpdateZoomSmoothing(dt);
		UpdatePositionSmoothing(dt);
		ClampCameraToBounds();
	}

	#region Input Handling
	private void HandleZoomInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
			return;

		if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			AdjustTargetZoom(zoomStep);
		else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			AdjustTargetZoom(-zoomStep);
	}

	private void HandleKeyboardZoomEvent(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
			return;

		if (keyEvent.Keycode == Key.Equal || keyEvent.Keycode == Key.KpAdd)
			AdjustTargetZoom(zoomStep);
		else if (keyEvent.Keycode == Key.Minus || keyEvent.Keycode == Key.KpSubtract)
			AdjustTargetZoom(-zoomStep);
	}

	private void HandleKeyboardZoomHold(float delta)
	{
		float holdDelta = zoomStep * keyboardZoomStepsPerSecond * delta;
		if (holdDelta <= 0.0f)
			return;

		if (Input.IsKeyPressed(Key.Equal) || Input.IsKeyPressed(Key.KpAdd))
			AdjustTargetZoom(holdDelta);
		else if (Input.IsKeyPressed(Key.Minus) || Input.IsKeyPressed(Key.KpSubtract))
			AdjustTargetZoom(-holdDelta);
	}

	private void HandleMiddleMouseState(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButton || mouseButton.ButtonIndex != MouseButton.Middle)
			return;

		_isMiddleMouseDragging = mouseButton.Pressed;
	}

	private void HandlePanInput(float delta)
	{
		Vector2 panDirection = Vector2.Zero;

		if (keyboardPanEnabled)
		{
			if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
				panDirection.X -= 1.0f;
			if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
				panDirection.X += 1.0f;
			if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
				panDirection.Y -= 1.0f;
			if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
				panDirection.Y += 1.0f;
		}

		if (panDirection == Vector2.Zero)
			return;

		float z = Mathf.Max(Zoom.X, 0.001f);
		_targetPosition += panDirection.Normalized() * (panSpeed * delta / z);
	}
	#endregion

	#region Zoom + Position Smoothing
	private void AdjustTargetZoom(float deltaZoom)
	{
		float clampedMaxZoom = ComputeEffectiveMaxZoom();
		_targetZoomScalar = Mathf.Clamp(_targetZoomScalar + deltaZoom, minZoom, clampedMaxZoom);
	}

	private void UpdateZoomSmoothing(float delta)
	{
		float clampedMaxZoom = ComputeEffectiveMaxZoom();
		_targetZoomScalar = Mathf.Clamp(_targetZoomScalar, minZoom, clampedMaxZoom);

		float t = zoomSmoothing <= 0.0f ? 1.0f : 1.0f - Mathf.Exp(-zoomSmoothing * delta);
		float currentScalar = Mathf.Lerp(Zoom.X, _targetZoomScalar, t);
		Zoom = new Vector2(currentScalar, currentScalar);
	}

	private void UpdatePositionSmoothing(float delta)
	{
		float t = positionSmoothing <= 0.0f ? 1.0f : 1.0f - Mathf.Exp(-positionSmoothing * delta);
		GlobalPosition = GlobalPosition.Lerp(_targetPosition, t);
	}
	#endregion

	#region Bounds Clamping
	private float ComputeEffectiveMaxZoom()
	{
		return maxZoom;
	}

	private void ClampCameraToBounds()
	{
		if (!enforceBounds)
			return;

		Rect2 bounds = GetPaddedBounds();
		Vector2 clamped = GlobalPosition;

		// Clamp camera center directly so panning limits stay the same at every zoom level.
		clamped.X = Mathf.Clamp(clamped.X, bounds.Position.X, bounds.End.X);
		clamped.Y = Mathf.Clamp(clamped.Y, bounds.Position.Y, bounds.End.Y);

		GlobalPosition = clamped;
		_targetPosition = clamped;
	}

	private Rect2 GetPaddedBounds()
	{
		float safePadding = Mathf.Max(boundsPadding, 0.0f);
		float safeExpansion = Mathf.Max(boundsExpansion, 0.0f);
		Vector2 min = worldBounds.Position - Vector2.One * safeExpansion + Vector2.One * safePadding;
		Vector2 size = worldBounds.Size + Vector2.One * (safeExpansion * 2.0f) - Vector2.One * (safePadding * 2.0f);
		size = new Vector2(Mathf.Max(1.0f, size.X), Mathf.Max(1.0f, size.Y));
		return new Rect2(min, size);
	}

	public void SetWorldBounds(Rect2 bounds)
	{
		worldBounds = bounds;
	}
	#endregion
}
