using Godot;
using System;
using System.Collections.Generic;

public partial class TimelineOverlay : CanvasLayer
{
	[Export] public int SlotCount = 4;
	[Export] public float SlotSpacing = 56.0f;
	[Export] public float IconSize = 48.0f;
	[Export] public float SlideDuration = 0.2f;

	private Control _queueRoot;
	private Control _iconsRoot;
	private Panel _currentHighlight;

	private readonly Dictionary<ulong, TextureRect> _iconNodesByEntityId = new Dictionary<ulong, TextureRect>();
	private Tween _activeTween;

	public override void _Ready()
	{
		_queueRoot = GetNode<Control>("%QueueRoot");
		_iconsRoot = GetNode<Control>("%IconsRoot");
		_currentHighlight = GetNode<Panel>("%CurrentHighlight");
		_currentHighlight.Size = new Vector2(IconSize, IconSize);
		_queueRoot.Position = _queueRoot.Position.Round();
	}


	public void SetQueue(List<GridEntity> queue)
	{
		if (queue == null) return;
		if (_queueRoot == null || _iconsRoot == null || _currentHighlight == null) return;

		// Clip the queue to the number of slots available
		List<GridEntity> clippedQueue = new List<GridEntity>();
		for (int i = 0; i < queue.Count && i < SlotCount; i++)
		{
			if (queue[i] != null && IsInstanceValid(queue[i]))
				clippedQueue.Add(queue[i]);
		}

		_currentHighlight.Visible = clippedQueue.Count > 0;
		_currentHighlight.Position = GetSlotPosition(0);
		_currentHighlight.Size = new Vector2(IconSize, IconSize);

		// Avoid overlapping tweens causing jitter/ghosting.
		_activeTween?.Kill();
		Tween updateTween = GetTree().CreateTween();
		updateTween.SetParallel(true);
		_activeTween = updateTween;

		// Loops over the shortened queue and adds entity to the timeline overlay order
		List<ulong> nextOrder = new List<ulong>();
		for (int i = 0; i < clippedQueue.Count; i++)
		{
			GridEntity entity = clippedQueue[i];
			ulong entityId = entity.GetInstanceId();
			nextOrder.Add(entityId);

			// Checks if the entity already exists in the timeline overlay
			bool alreadyExists = _iconNodesByEntityId.TryGetValue(entityId, out TextureRect iconNode);
			if (!alreadyExists)
				iconNode = GetOrCreateIconNode(entityId);

			iconNode.Texture = BuildIconTexture(entity.sprite);

			Vector2 endPos = GetSlotPosition(i);
			iconNode.Visible = true;

			// If the entity doesn't exist in the timeline overlay, it slides in from the right
			if (!alreadyExists)
			{
				// New icons slide/fade in from the right.
				iconNode.Position = endPos + new Vector2(18, 0);
				iconNode.Modulate = new Color(1, 1, 1, 0);
			}
			else
			{
				iconNode.Modulate = Colors.White;
			}

			// Moves the sprite icon to the correct position based on the index of the entity in the queue
			AnimateIcon(updateTween, iconNode, endPos, 1.0f);
		}

		List<ulong> toRemove = new List<ulong>();
		foreach (KeyValuePair<ulong, TextureRect> pair in _iconNodesByEntityId)
		{
			if (!nextOrder.Contains(pair.Key))
			{
				AnimateIcon(updateTween, pair.Value, pair.Value.Position + new Vector2(-20, 0), 0.0f, () => pair.Value.QueueFree());
				toRemove.Add(pair.Key);
			}
		}

		foreach (ulong removedId in toRemove)
			_iconNodesByEntityId.Remove(removedId);

	}

	// Creates a new icon node for the entity if it doesn't exist, otherwise returns the existing node
	private TextureRect GetOrCreateIconNode(ulong entityId)
	{
		if (_iconNodesByEntityId.TryGetValue(entityId, out TextureRect existing))
			return existing;

		TextureRect iconNode = new TextureRect();
		iconNode.Name = $"EntityIcon_{entityId}";
		iconNode.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
		iconNode.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		iconNode.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		iconNode.Size = new Vector2(IconSize, IconSize);
		iconNode.Position = iconNode.Position.Round();
		iconNode.MouseFilter = Control.MouseFilterEnum.Ignore;
		_iconsRoot.AddChild(iconNode);

		_iconNodesByEntityId[entityId] = iconNode;
		return iconNode;
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

