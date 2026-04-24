using Godot;
using System;
using System.Collections.Generic;

public partial class TimelineOverlay : CanvasLayer
{
	[Export] public int SlotCount = 4;
	[Export] public float SlotSpacing = 58.0f;
	[Export] public float IconSize = 50.0f;
	[Export] public float SlideDuration = 0.2f;

	private Control _queueRoot;
	private Control _iconsRoot;
	private Panel _currentHighlight;

	// One icon per timeline slot so the same entity can appear in multiple slots.
	private TextureRect[] _slotIcons;

	// Instance id last shown in each slot; 0 means none. Used to slide in from the right when the occupant changes.
	private ulong[] _slotLastEntityId;

	private Tween _activeTween;

	public override void _Ready()
	{
		_queueRoot = GetNode<Control>("%QueueRoot");
		_iconsRoot = GetNode<Control>("%IconsRoot");
		_currentHighlight = GetNode<Panel>("%CurrentHighlight");
		_queueRoot.Position = _queueRoot.Position.Round();
	}


	public void SetQueue(List<GridEntity> queue)
	{
		if (queue == null) return;
		if (_queueRoot == null || _iconsRoot == null || _currentHighlight == null) return;

		// Clip the queue to the number of slots available; first occurrence of each entity id wins
		List<GridEntity> clippedQueue = new List<GridEntity>();
		HashSet<ulong> seenEntityIds = new HashSet<ulong>();
		for (int i = 0; i < queue.Count && clippedQueue.Count < SlotCount; i++)
		{
			if (queue[i] == null || !IsInstanceValid(queue[i]))
				continue;
			ulong id = queue[i].GetInstanceId();
			if (seenEntityIds.Contains(id))
				continue;
			seenEntityIds.Add(id);
			clippedQueue.Add(queue[i]);
		}

		_currentHighlight.Visible = clippedQueue.Count > 0;
		_currentHighlight.Position = GetSlotPosition(0);

		// Avoid overlapping tweens causing jitter/ghosting.
		_activeTween?.Kill();
		EnsureSlotIcons();
		Tween updateTween = GetTree().CreateTween();
		updateTween.SetParallel(true);
		_activeTween = updateTween;

		for (int i = 0; i < clippedQueue.Count; i++)
		{
			GridEntity entity = clippedQueue[i];
			TextureRect iconNode = GetSlotIcon(i);
			ulong entityId = entity.GetInstanceId();

			iconNode.Texture = BuildIconTexture(entity.sprite);

			Vector2 endPos = GetSlotPosition(i);
			// Slide in from the right when this slot was empty/hidden or is showing a different entity than last frame
			// (per-slot nodes reuse the same Control when the queue reorders).
			bool slideInFromRight = !iconNode.Visible || iconNode.Modulate.A < 0.01f
				|| entityId != _slotLastEntityId[i];
			iconNode.Visible = true;

			if (slideInFromRight)
			{
				iconNode.Position = endPos + new Vector2(18, 0);
				iconNode.Modulate = new Color(1, 1, 1, 0);
			}
			else
			{
				iconNode.Modulate = Colors.White;
			}

			AnimateIcon(updateTween, iconNode, endPos, 1.0f);
			_slotLastEntityId[i] = entityId;
		}

		for (int i = clippedQueue.Count; i < SlotCount; i++)
		{
			TextureRect iconNode = GetSlotIcon(i);
			_slotLastEntityId[i] = 0;
			Vector2 startPos = iconNode.Position;
			AnimateIcon(updateTween, iconNode, startPos + new Vector2(-20, 0), 0.0f, () =>
			{
				iconNode.Visible = false;
			});
		}
	}

	private void EnsureSlotIcons()
	{
		if (_slotIcons != null && _slotIcons.Length == SlotCount)
			return;

		if (_iconsRoot != null)
		{
			foreach (Node child in _iconsRoot.GetChildren())
				child.QueueFree();
		}

		_slotIcons = new TextureRect[SlotCount];
		_slotLastEntityId = new ulong[SlotCount];
		for (int i = 0; i < SlotCount; i++)
		{
			TextureRect iconNode = new TextureRect();
			iconNode.Name = $"TimelineSlot_{i}";
			iconNode.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
			iconNode.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			iconNode.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			iconNode.Size = new Vector2(IconSize, IconSize);
			iconNode.Position = iconNode.Position.Round();
			iconNode.MouseFilter = Control.MouseFilterEnum.Ignore;
			iconNode.Visible = false;
			_iconsRoot.AddChild(iconNode);
			_slotIcons[i] = iconNode;
		}
	}

	private TextureRect GetSlotIcon(int slotIndex)
	{
		EnsureSlotIcons();
		return _slotIcons[slotIndex];
	}

	// Gets the position of the slot in the timeline UI
	private Vector2 GetSlotPosition(int slotIndex)
	{
		return new Vector2(Mathf.Round(slotIndex * SlotSpacing), 0);
	}

	// Pulls a single frame from the sprite sheet and returns it as a texture
	// Makes it so that the icon is the correct size and frame of the sprite sheet
	private Texture2D BuildIconTexture(Sprite2D sprite)
	{
		if (sprite == null || sprite.Texture == null) return null;

		if (sprite.RegionEnabled)
		{
			AtlasTexture regionTexture = new AtlasTexture();
			regionTexture.Atlas = sprite.Texture;
			regionTexture.Region = sprite.RegionRect;
			return regionTexture;
		}

		int hframes = Math.Max(1, sprite.Hframes);
		int vframes = Math.Max(1, sprite.Vframes);
		if (hframes == 1 && vframes == 1)
			return sprite.Texture;

		Vector2 textureSize = sprite.Texture.GetSize();
		int frameWidth = (int)textureSize.X / hframes;
		int frameHeight = (int)textureSize.Y / vframes;
		int frame = Mathf.Clamp(sprite.Frame, 0, (hframes * vframes) - 1);
		int frameX = frame % hframes;
		int frameY = frame / hframes;

		AtlasTexture frameTexture = new AtlasTexture();
		frameTexture.Atlas = sprite.Texture;
		frameTexture.Region = new Rect2(frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight);
		return frameTexture;
	}

	// Animates the icon node to the target position and alpha
	private void AnimateIcon(Tween tween, TextureRect iconNode, Vector2 targetPosition, float targetAlpha, Action onFinished = null)
	{
		targetPosition = targetPosition.Round();
		tween.TweenProperty(iconNode, "position", targetPosition, SlideDuration)
			.SetTrans(Tween.TransitionType.Quint)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(iconNode, "modulate:a", targetAlpha, SlideDuration);
		if (onFinished != null)
			tween.Finished += onFinished;
	}
}
