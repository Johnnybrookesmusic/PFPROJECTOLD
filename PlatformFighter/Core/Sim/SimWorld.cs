using System;
using System.Collections.Generic;
using PlatformFighter.Core.Input;
using PlatformFighter.Core.Math;

namespace PlatformFighter.Core.Sim;

/// <summary>
/// The authoritative game state container. Owns every ISimObject and
/// advances them exactly one frame per Tick() call.
///
/// This class is a plain C# object — deliberately NOT a Node — so it can
/// be copied, serialized, and re-simulated by rollback netcode without
/// touching the scene tree. Phase 2 makes good on that: SaveState/
/// LoadState + CreateSnapshot/RestoreSnapshot + ComputeStateHash below.
/// </summary>
public sealed class SimWorld
{
	/// <summary>Simulation rate. A "frame" in fighting-game terms.</summary>
	public const int TicksPerSecond = 60;

	/// <summary>Fixed delta, 1/60 s, as an exact fixed-point ratio.</summary>
	public static readonly Fx DeltaTime = Fx.Ratio(1, TicksPerSecond);

	/// <summary>
	/// Fixed slot count for player inputs. Unused slots simply latch
	/// FrameInput.None every tick — cheap and harmless.
	/// </summary>
	public const int MaxPlayers = 4;

	/// <summary>Monotonic frame counter. Frame 0 = initial state.</summary>
	public int FrameNumber { get; private set; }

	/// <summary>World-owned RNG so random events replay identically.</summary>
	public DeterministicRandom Rng;

	// Ordered list => deterministic tick order. Never use an unordered set here.
	private readonly List<ISimObject> _objects = new();

	// Structural changes are queued and applied at the start of the NEXT
	// Tick(), never mid-iteration (see Phase 1 notes). CreateSnapshot()
	// also applies them first — equivalent timing, since nothing between
	// ticks reads _objects except the render, and it keeps pending queues
	// out of the snapshot format entirely.
	private readonly List<ISimObject> _pendingAdds = new();
	private readonly List<ISimObject> _pendingRemoves = new();

	private readonly FrameInput[] _currentInputs = new FrameInput[MaxPlayers];

	// Reused for per-frame hashing so hashing doesn't allocate.
	private readonly StateWriter _hashScratch = new();

	public SimWorld(ulong seed)
	{
		Rng = new DeterministicRandom(seed);
	}

	public void Register(ISimObject obj) => _pendingAdds.Add(obj);

	public void Unregister(ISimObject obj) => _pendingRemoves.Add(obj);

	/// <summary>Latched input for a player slot during the CURRENT tick.</summary>
	public FrameInput GetInput(int playerIndex) => _currentInputs[playerIndex];

	public void Tick(ReadOnlySpan<FrameInput> inputs)
	{
		ApplyPendingStructuralChanges();

		for (int p = 0; p < MaxPlayers; p++)
			_currentInputs[p] = p < inputs.Length ? inputs[p] : FrameInput.None;

		FrameNumber++;

		for (int i = 0; i < _objects.Count; i++)
		{
			_objects[i].Tick(this);
		}
	}

	private void ApplyPendingStructuralChanges()
	{
		if (_pendingRemoves.Count > 0)
		{
			for (int i = 0; i < _pendingRemoves.Count; i++)
				_objects.Remove(_pendingRemoves[i]);
			_pendingRemoves.Clear();
		}

		if (_pendingAdds.Count > 0)
		{
			_objects.AddRange(_pendingAdds);
			_pendingAdds.Clear();
		}
	}

	// ---- State serialization / hashing / snapshots ---------------------

	/// <summary>
	/// Serialize the COMPLETE gameplay state: frame counter, RNG, latched
	/// inputs, and every object (type id + state) in tick order. Object
	/// ordering is captured implicitly and exactly — it IS the write order.
	/// Rendering data is excluded by construction: nothing view-side is
	/// reachable from here.
	/// </summary>
	public void SaveState(StateWriter w)
	{
		w.WriteInt(FrameNumber);
		w.WriteULong(Rng.State);

		for (int p = 0; p < MaxPlayers; p++)
			w.WriteULong(_currentInputs[p].Pack()); // ulong-wide slot; survives the 2.3 format widening

		w.WriteInt(_objects.Count);
		for (int i = 0; i < _objects.Count; i++)
		{
			w.WriteInt(_objects[i].TypeId);
			_objects[i].SaveState(w);
		}
	}

	/// <summary>
	/// Mirror of SaveState. Reuses live objects in place when the type at
	/// that position matches (keeps view references valid), otherwise
	/// rebuilds from the SimObjectTypes factory.
	/// </summary>
	public void LoadState(StateReader r)
	{
		_pendingAdds.Clear();
		_pendingRemoves.Clear();

		FrameNumber = r.ReadInt();
		Rng.State = r.ReadULong();

		for (int p = 0; p < MaxPlayers; p++)
		  _currentInputs[p] = FrameInput.Unpack(r.ReadULong());

		int count = r.ReadInt();
		for (int i = 0; i < count; i++)
		{
			int typeId = r.ReadInt();
			ISimObject obj;
			if (i < _objects.Count && _objects[i].TypeId == typeId)
			{
				obj = _objects[i];
			}
			else
			{
				obj = SimObjectTypes.Create(typeId);
				if (i < _objects.Count) _objects[i] = obj;
				else _objects.Add(obj);
			}
			obj.LoadState(r);
		}

		if (_objects.Count > count)
			_objects.RemoveRange(count, _objects.Count - count);
	}

	/// <summary>Hash of the complete current gameplay state. Allocation-free.</summary>
	public ulong ComputeStateHash()
	{
		_hashScratch.Reset();
		SaveState(_hashScratch);
		return StateHash.Compute(_hashScratch.AsSpan());
	}

	public Snapshot CreateSnapshot()
	{
		ApplyPendingStructuralChanges();
		var w = new StateWriter();
		SaveState(w);
		byte[] data = w.ToArray();
		return new Snapshot(FrameNumber, data, StateHash.Compute(data));
	}

	public void RestoreSnapshot(Snapshot s) => LoadState(new StateReader(s.Data));
}
