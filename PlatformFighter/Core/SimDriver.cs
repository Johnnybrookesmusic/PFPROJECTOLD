using System;
using Godot;
using PlatformFighter.Core.Input;
using PlatformFighter.Core.Sim;

namespace PlatformFighter.Core;

/// <summary>
/// Drives SimWorld at a fixed 60 Hz using the accumulator pattern,
/// independent of render FPS. This is also the ONLY place input is
/// sampled from Godot — see Docs/INPUT.md for the full contract.
/// </summary>
public partial class SimDriver : Node
{
	public const double TickDuration = 1.0 / SimWorld.TicksPerSecond;

	private const int MaxTicksPerRenderFrame = 6;

	public SimWorld World { get; private set; } = null!;

	public float RenderAlpha { get; private set; }

	public int TicksLastFrame { get; private set; }

	public IInputProvider?[] Providers { get; } = new IInputProvider[SimWorld.MaxPlayers];

	public InputRingBuffer[] InputHistory { get; } = new InputRingBuffer[SimWorld.MaxPlayers];

	/// <summary>Per-frame state hashes — the desync tripwire (Phase 2.1).</summary>
	public HashHistory Hashes { get; } = new();

	/// <summary>Hash of sim state after the most recent tick, for the debug overlay.</summary>
	public ulong LatestHash { get; private set; }

	/// <summary>
	/// Hashing serializes the whole world every tick. Trivial now; if it
	/// ever shows up in a profile, flip this off for release builds — but
	/// keep it ON for all of development. Silent nondeterminism is the
	/// most expensive bug this project can have.
	/// </summary>
	public bool HashEveryFrame { get; set; } = true;

	private double _accumulator;

	public override void _Ready()
	{
		World = new SimWorld(seed: 1);

		for (int p = 0; p < SimWorld.MaxPlayers; p++)
			InputHistory[p] = new InputRingBuffer();
	}
public void SetProvider(int playerIndex, IInputProvider? provider) => Providers[playerIndex] = provider;
	public override void _Process(double delta)
	{
		_accumulator += System.Math.Min(delta, 0.25);

		TicksLastFrame = 0;
		while (_accumulator >= TickDuration && TicksLastFrame < MaxTicksPerRenderFrame)
		{
			RunOneTick();
			_accumulator -= TickDuration;
			TicksLastFrame++;
		}

		if (_accumulator >= TickDuration)
			_accumulator = 0;

		RenderAlpha = (float)(_accumulator / TickDuration);
	}

	private void RunOneTick()
	{
		Span<FrameInput> frameInputs = stackalloc FrameInput[SimWorld.MaxPlayers];
		for (int p = 0; p < SimWorld.MaxPlayers; p++)
			frameInputs[p] = Providers[p]?.Sample() ?? FrameInput.None;

		World.Tick(frameInputs);

		int frame = World.FrameNumber;
		for (int p = 0; p < SimWorld.MaxPlayers; p++)
			InputHistory[p].Record(frame, frameInputs[p]);

		if (HashEveryFrame)
		{
			LatestHash = World.ComputeStateHash();
			Hashes.Record(frame, LatestHash);
		}
	}
}
