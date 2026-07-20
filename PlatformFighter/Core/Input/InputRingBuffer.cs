using System;

namespace PlatformFighter.Core.Input;

/// <summary>
/// Fixed-capacity circular history of FrameInput keyed by absolute sim
/// frame number, one instance per player. This is local input history
/// today (useful for debugging and eventually replay files); Phase 6
/// rollback netcode will read and rewrite these same buffers to hold
/// predicted vs. confirmed remote input, so the shape is deliberately
/// "frame number in, FrameInput out" rather than anything queue-like.
/// </summary>
public sealed class InputRingBuffer
{
    /// <summary>~2.1s of history at 60Hz. Power-of-two so lookup is a mask, not a modulo.</summary>
    public const int DefaultCapacity = 128;

    private readonly FrameInput[] _slots;
    private readonly int[] _slotFrame; // which frame currently occupies each slot; -1 = empty
    private readonly int _mask;

    public InputRingBuffer(int capacity = DefaultCapacity)
    {
        if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
            throw new ArgumentException("capacity must be a power of two", nameof(capacity));

        _slots = new FrameInput[capacity];
        _slotFrame = new int[capacity];
        Array.Fill(_slotFrame, -1);
        _mask = capacity - 1;
    }

    public void Record(int frameNumber, FrameInput input)
    {
        int idx = frameNumber & _mask;
        _slots[idx] = input;
        _slotFrame[idx] = frameNumber;
    }

    /// <summary>
    /// True and populated if this exact frame's input is still in the
    /// buffer (i.e. hasn't been overwritten by a later frame yet).
    /// </summary>
    public bool TryGet(int frameNumber, out FrameInput input)
    {
        int idx = frameNumber & _mask;
        if (_slotFrame[idx] == frameNumber)
        {
            input = _slots[idx];
            return true;
        }

        input = FrameInput.None;
        return false;
    }
}
