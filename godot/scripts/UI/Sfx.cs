using System.Collections.Generic;
using Godot;

namespace Panacea.UI;

/// <summary>
/// The shop's sounds (synthesised by tools/synth_sounds.py). One-shots play from a small pool,
/// with a touch of pitch variation so repeated taps don't sound stamped out; loops (the room,
/// the press, Death's breath) fade in and out. Sound can be switched off from the footer.
/// </summary>
public partial class Sfx : Node
{
	private static Sfx? _i;
	private readonly List<AudioStreamPlayer> _pool = new();
	private readonly Dictionary<string, AudioStream> _cache = new();
	private readonly Dictionary<string, AudioStreamPlayer> _loops = new();
	private readonly RandomNumberGenerator _rng = new() { Seed = 1885 };
	public static bool Enabled { get; private set; } = true;

	public override void _Ready()
	{
		_i = this;
		for (var n = 0; n < 14; n++)
		{
			var p = new AudioStreamPlayer();
			AddChild(p);
			_pool.Add(p);
		}
	}

	private AudioStream? Stream(string name, bool loop = false)
	{
		if (_cache.TryGetValue(name, out var s)) return s;
		var path = $"res://assets/sound/{name}.wav";
		if (!ResourceLoader.Exists(path)) return null;
		var stream = GD.Load<AudioStream>(path);
		if (loop && stream is AudioStreamWav wav)
		{
			wav = (AudioStreamWav)wav.Duplicate();
			wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
			wav.LoopBegin = 0;
			wav.LoopEnd = (int)(wav.GetLength() * wav.MixRate);
			stream = wav;
		}
		_cache[name] = stream;
		return stream;
	}

	/// <summary>Plays a one-shot. <paramref name="jitter"/> varies the pitch a little each time.</summary>
	public static void Play(string name, float db = 0f, float jitter = 0.04f, float pitch = 1f)
	{
		if (_i == null || !Enabled) return;
		var s = _i.Stream(name);
		if (s == null) return;
		AudioStreamPlayer? free = null;
		foreach (var p in _i._pool) if (!p.Playing) { free = p; break; }
		free ??= _i._pool[0];
		free.Stream = s;
		free.VolumeDb = db;
		free.PitchScale = pitch * (1f + _i._rng.RandfRange(-jitter, jitter));
		free.Play();
	}

	/// <summary>Plays <paramref name="name"/> after <paramref name="seconds"/>.</summary>
	public static void After(float seconds, string name, float db = 0f)
	{
		if (_i == null) return;
		_i.GetTree().CreateTimer(seconds).Timeout += () => Play(name, db);
	}

	/// <summary>Starts (or keeps) a loop, fading it to <paramref name="db"/>.</summary>
	public static void Loop(string name, float db, float fadeSeconds = 0.6f)
	{
		if (_i == null) return;
		if (!_i._loops.TryGetValue(name, out var p))
		{
			var s = _i.Stream(name, loop: true);
			if (s == null) return;
			p = new AudioStreamPlayer { Stream = s, VolumeDb = -60 };
			_i.AddChild(p);
			_i._loops[name] = p;
		}
		p.SetMeta("target", db);
		if (!Enabled) return;
		if (!p.Playing) { p.VolumeDb = -60; p.Play(); }
		_i.CreateTween().TweenProperty(p, "volume_db", db, fadeSeconds);
	}

	public static void StopLoop(string name, float fadeSeconds = 0.6f)
	{
		if (_i == null || !_i._loops.TryGetValue(name, out var p) || !p.Playing) return;
		var t = _i.CreateTween();
		t.TweenProperty(p, "volume_db", -60f, fadeSeconds);
		t.TweenCallback(Callable.From(() => { if (p.VolumeDb <= -59f) p.Stop(); }));
	}

	public static void SetEnabled(bool on)
	{
		Enabled = on;
		if (_i == null) return;
		AudioServer.SetBusMute(0, !on);
		if (on)
			foreach (var (name, p) in _i._loops)
				if (p.HasMeta("target") && !p.Playing) Loop(name, (float)p.GetMeta("target"));
	}
}
