using Godot;

namespace Panacea;

/// <summary>
/// Beauty-corner spike: builds the shop counter at night in code so it can be iterated from
/// screenshots. Not the production scene structure — once the look is approved, the pieces move
/// into authored .tscn scenes.
/// Coordinates: the proprietor (camera) stands at +Z behind the counter; the customer and the
/// shop window are toward -Z.
/// </summary>
public partial class ShopBuilder : Node3D
{
	private const string Ph = "res://assets/polyhaven/";
	private const float CounterTop = 0.98f;
	private const float LabelYaw = 130f;
	private static readonly Vector3 Glass0 = new(-0.2f, CounterTop, 0.24f); // yaw that turns a label's centre toward +Z

	public override void _Ready()
	{
		BuildEnvironment();
		BuildRoom();
		BuildCounter();
		BuildProps();
		BuildBottles();
		BuildTincture(Glass0);
		BuildCustomer(new Vector3(-0.6f, 0f, -0.95f));
		BuildLights();
		BuildCamera();
		BuildGrade();
	}

	// ---------- helpers ----------

	private static Texture2D Tex(string id, string map) => GD.Load<Texture2D>($"{Ph}{id}/{map}.jpg");

	private static StandardMaterial3D Pbr(string id, float scale, Color? tint = null)
	{
		var m = new StandardMaterial3D
		{
			AlbedoTexture = Tex(id, "albedo"),
			AlbedoColor = tint ?? Colors.White,
			NormalEnabled = true,
			NormalTexture = Tex(id, "normal"),
			RoughnessTexture = Tex(id, "roughness"),
			AOEnabled = true,
			AOTexture = Tex(id, "ao"),
			Uv1Triplanar = true,
			Uv1Scale = Vector3.One * scale,
		};
		return m;
	}

	private MeshInstance3D Box(Vector3 size, Vector3 center, Material mat, Node3D? parent = null)
	{
		var mi = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = center, MaterialOverride = mat };
		(parent ?? this).AddChild(mi);
		return mi;
	}

	private Node3D Prop(string id, Vector3 pos, float rotY = 0f, float scale = 1f, Vector3? rot = null)
	{
		var scene = GD.Load<PackedScene>($"{Ph}{id}/{id}.gltf");
		var n = scene.Instantiate<Node3D>();
		n.Position = pos;
		n.RotationDegrees = rot ?? new Vector3(0, rotY, 0);
		n.Scale = Vector3.One * scale;
		AddChild(n);
		return n;
	}

	/// <summary>Turns a (radius, height) profile, bottom to top, into a closed surface of revolution.</summary>
	private static ArrayMesh Lathe(Vector2[] profile, int segments = 40)
	{
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		var n = profile.Length;
		var normals = new Vector2[n];
		var v = new float[n];
		for (var i = 0; i < n; i++)
		{
			var a = profile[Mathf.Max(i - 1, 0)];
			var b = profile[Mathf.Min(i + 1, n - 1)];
			var d = (b - a).Normalized();
			normals[i] = new Vector2(d.Y, -d.X);
			v[i] = i == 0 ? 0 : v[i - 1] + profile[i].DistanceTo(profile[i - 1]);
		}
		for (var i = 0; i < n; i++) v[i] /= v[n - 1];
		Vector3 P(int i, int j) { var t = Mathf.Tau * j / segments; return new Vector3(Mathf.Cos(t) * profile[i].X, profile[i].Y, Mathf.Sin(t) * profile[i].X); }
		Vector3 N(int i, int j) { var t = Mathf.Tau * j / segments; return new Vector3(Mathf.Cos(t) * normals[i].X, normals[i].Y, Mathf.Sin(t) * normals[i].X).Normalized(); }
		void Vert(int i, int j) { st.SetNormal(N(i, j)); st.SetUV(new Vector2((float)j / segments, 1 - v[i])); st.AddVertex(P(i, j)); }
		for (var i = 0; i < n - 1; i++)
			for (var j = 0; j < segments; j++)
			{
				Vert(i, j); Vert(i + 1, j + 1); Vert(i + 1, j);
				Vert(i, j); Vert(i, j + 1); Vert(i + 1, j + 1);
			}
		return st.Commit();
	}

	private static Vector2[] V(params float[] xy)
	{
		var r = new Vector2[xy.Length / 2];
		for (var i = 0; i < r.Length; i++) r[i] = new Vector2(xy[2 * i], xy[2 * i + 1]);
		return r;
	}

	private static StandardMaterial3D Glass(Color tint, float alpha) => new()
	{
		Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		AlbedoColor = new Color(tint, alpha),
		Roughness = 0.03f,
		Metallic = 0.0f,
		MetallicSpecular = 0.9f,
		RimEnabled = true,
		Rim = 0.25f,
		RimTint = 0.1f,
		CullMode = BaseMaterial3D.CullModeEnum.Disabled,
	};

	// ---------- scene ----------

	private void BuildEnvironment()
	{
		var env = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = new Color(0.005f, 0.006f, 0.01f),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.16f, 0.13f, 0.12f),
			AmbientLightEnergy = 0.25f,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
			TonemapExposure = 1.1f,
			TonemapWhite = 6f,
			SsaoEnabled = true,
			SsaoRadius = 0.6f,
			SsaoIntensity = 2.5f,
			GlowEnabled = true,
			GlowIntensity = 0.9f,
			GlowBloom = 0.06f,
			GlowHdrThreshold = 0.9f,
			GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Softlight,
			VolumetricFogEnabled = true,
			VolumetricFogDensity = 0.022f,
			VolumetricFogAlbedo = new Color(0.85f, 0.8f, 0.72f),
			VolumetricFogAnisotropy = 0.55f,
			VolumetricFogLength = 10f,
			VolumetricFogTemporalReprojectionEnabled = true,
			AdjustmentEnabled = true,
			AdjustmentContrast = 1.08f,
			AdjustmentSaturation = 0.95f,
		};
		AddChild(new WorldEnvironment { Environment = env });
	}

	private void BuildRoom()
	{
		var floor = Pbr("dark_wooden_planks", 0.7f);
		var paper = Pbr("decrepit_wallpaper", 0.8f, new Color(0.75f, 0.68f, 0.6f));
		var panel = Pbr("dark_paneled_wood", 0.9f);
		var dark = new StandardMaterial3D { AlbedoColor = new Color(0.03f, 0.025f, 0.02f), Roughness = 0.9f };

		const float x0 = -2.6f, x1 = 2.6f, z0 = -3.4f, z1 = 2.2f, h = 3.3f;
		Box(new Vector3(x1 - x0, 0.1f, z1 - z0), new Vector3(0, -0.05f, (z0 + z1) / 2), floor);
		Box(new Vector3(x1 - x0, 0.1f, z1 - z0), new Vector3(0, h + 0.05f, (z0 + z1) / 2), dark);

		// side walls: wallpaper above a panelled dado
		foreach (var x in new[] { x0, x1 })
		{
			var sx = x < 0 ? -0.05f : 0.05f;
			Box(new Vector3(0.1f, h, z1 - z0), new Vector3(x + sx, h / 2, (z0 + z1) / 2), paper);
			Box(new Vector3(0.06f, 1.1f, z1 - z0), new Vector3(x - sx * 0.2f, 0.55f, (z0 + z1) / 2), panel);
		}
		Box(new Vector3(x1 - x0, h, 0.1f), new Vector3(0, h / 2, z1 + 0.05f), paper);

		// front wall with the shop window (opening x -1.7..0.9, y 0.75..2.7)
		const float wx0 = -1.7f, wx1 = 0.9f, wy0 = 0.75f, wy1 = 2.7f;
		Box(new Vector3(wx0 - x0, h, 0.2f), new Vector3((x0 + wx0) / 2, h / 2, z0), panel);
		Box(new Vector3(x1 - wx1, h, 0.2f), new Vector3((x1 + wx1) / 2, h / 2, z0), panel);
		Box(new Vector3(wx1 - wx0, wy0, 0.2f), new Vector3((wx0 + wx1) / 2, wy0 / 2, z0), panel);
		Box(new Vector3(wx1 - wx0, h - wy1, 0.2f), new Vector3((wx0 + wx1) / 2, (h + wy1) / 2, z0), panel);

		// mullions
		var frame = Pbr("dark_wood", 2f, new Color(0.5f, 0.4f, 0.32f));
		for (var i = 0; i <= 3; i++)
		{
			var x = Mathf.Lerp(wx0, wx1, i / 3f);
			Box(new Vector3(0.06f, wy1 - wy0, 0.08f), new Vector3(x, (wy0 + wy1) / 2, z0 + 0.06f), frame);
		}
		Box(new Vector3(wx1 - wx0, 0.06f, 0.08f), new Vector3((wx0 + wx1) / 2, 2.05f, z0 + 0.06f), frame);

		// the street beyond the glass
		var street = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/window.gdshader") };
		var backdrop = new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = new Vector2(5f, 3.2f) },
			Position = new Vector3((wx0 + wx1) / 2, 1.7f, z0 - 1.6f),
			MaterialOverride = street,
		};
		AddChild(backdrop);
		var pane = Glass(new Color(0.7f, 0.8f, 0.85f), 0.025f);
		pane.RimEnabled = false;
		Box(new Vector3(wx1 - wx0, wy1 - wy0, 0.01f), new Vector3((wx0 + wx1) / 2, (wy0 + wy1) / 2, z0 + 0.02f), pane);

		// gilt window lettering, read backwards from inside the shop
		var font = GD.Load<FontFile>("res://assets/fonts/IMFeENrm28P.ttf");
		var gilt = new Label3D
		{
			Text = "DR. HALLORAN'S\nPANACEA",
			Font = font,
			FontSize = 128,
			PixelSize = 0.0024f,
			Modulate = new Color(0.62f, 0.44f, 0.18f),
			OutlineSize = 6,
			OutlineModulate = new Color(0.08f, 0.05f, 0.02f),
			Shaded = false,
			LineSpacing = -30,
			Position = new Vector3((wx0 + wx1) / 2 + 0.05f, 1.78f, z0 + 0.03f),
			Scale = new Vector3(-1, 1, 1),
		};
		AddChild(gilt);
	}

	private void BuildCounter()
	{
		var top = Pbr("dark_wood", 1.2f, new Color(0.85f, 0.72f, 0.6f));
		var body = Pbr("dark_paneled_wood", 1.0f);
		Box(new Vector3(3.2f, 0.05f, 0.72f), new Vector3(0.05f, CounterTop - 0.025f, 0f), top);
		Box(new Vector3(3.0f, CounterTop - 0.05f, 0.6f), new Vector3(0.05f, (CounterTop - 0.05f) / 2, 0f), body);
		var brass = new StandardMaterial3D { AlbedoColor = new Color(0.78f, 0.58f, 0.28f), Metallic = 1f, Roughness = 0.35f };
		Box(new Vector3(3.2f, 0.015f, 0.02f), new Vector3(0.05f, CounterTop - 0.06f, -0.37f), brass);
	}

	private void BuildProps()
	{
		Prop("vintage_cabinet_01", new Vector3(-2.25f, 0f, -0.7f), 90f);
		Prop("vintage_oil_lamp", new Vector3(-0.85f, CounterTop, -0.05f), 20f);
		Prop("brass_pot_01", new Vector3(-0.35f, CounterTop, -0.12f), 0f, 0.7f);
		Prop("magnifying_glass_01", new Vector3(0.45f, CounterTop + 0.014f, 0.2f), 0f, 1f, new Vector3(-90, 0, 35));
		Prop("mantel_clock_01", new Vector3(1.35f, CounterTop + 0.054f, -0.1f), -25f);
		Prop("antique_ceramic_vase_01", new Vector3(-2.2f, 2.24f, -0.2f), 0f, 0.8f);
		Prop("Lantern_01", new Vector3(1.7f, CounterTop, 0.05f), -10f);
	}

	private void BuildBottles()
	{
		var tints = new[]
		{
			new Color(0.45f, 0.22f, 0.05f), new Color(0.08f, 0.16f, 0.45f), new Color(0.12f, 0.32f, 0.12f),
			new Color(0.5f, 0.08f, 0.08f), new Color(0.6f, 0.45f, 0.15f),
		};
		var rng = new RandomNumberGenerator { Seed = 1885 };
		var cork = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.33f, 0.2f), Roughness = 0.95f };
		var label = new StandardMaterial3D { AlbedoColor = new Color(0.86f, 0.8f, 0.64f), Roughness = 0.9f };

		// Three period shapes: a tall Boston round, a squat panel bottle, a slim tincture vial.
		var shapes = new[]
		{
			V(0f,0f, 0.04f,0f, 0.045f,0.006f, 0.045f,0.15f, 0.04f,0.172f, 0.022f,0.19f, 0.014f,0.2f, 0.013f,0.235f, 0.017f,0.24f, 0.017f,0.25f, 0f,0.25f),
			V(0f,0f, 0.052f,0f, 0.056f,0.008f, 0.056f,0.1f, 0.05f,0.118f, 0.02f,0.132f, 0.016f,0.16f, 0.02f,0.165f, 0.02f,0.172f, 0f,0.172f),
			V(0f,0f, 0.026f,0f, 0.028f,0.005f, 0.028f,0.17f, 0.022f,0.185f, 0.011f,0.195f, 0.01f,0.225f, 0.013f,0.23f, 0.013f,0.238f, 0f,0.238f),
		};
		var shapeMeshes = new ArrayMesh[shapes.Length];
		var fillMeshes = new ArrayMesh[shapes.Length];
		for (var k = 0; k < shapes.Length; k++)
		{
			shapeMeshes[k] = Lathe(shapes[k]);
			var body = shapes[k][3]; // top of the straight body
			fillMeshes[k] = Lathe(V(0f, 0.004f, body.X - 0.004f, 0.004f, body.X - 0.004f, body.Y * 0.85f, 0f, body.Y * 0.85f), 28);
		}
		var labelShader = GD.Load<Shader>("res://shaders/label.gdshader");
		var accents = new[] { new Color(0.55f, 0.1f, 0.06f), new Color(0.1f, 0.18f, 0.4f), new Color(0.12f, 0.1f, 0.08f) };

		void Bottle(Vector3 at, float scale, int tint, int shape = -1, float facing = 0f)
		{
			var k = shape >= 0 ? shape : rng.RandiRange(0, shapes.Length - 1);
			var root = new Node3D { Position = at, Scale = Vector3.One * scale, RotationDegrees = new Vector3(0, facing + rng.RandfRange(-25, 25), 0) };
			AddChild(root);
			var c = tints[tint % tints.Length];
			var liquid = new StandardMaterial3D
			{
				AlbedoColor = c.Darkened(0.35f),
				EmissionEnabled = true,
				Emission = c,
				EmissionEnergyMultiplier = 0.12f,
				Roughness = 0.1f,
				MetallicSpecular = 0.8f,
			};
			root.AddChild(new MeshInstance3D { Mesh = fillMeshes[k], MaterialOverride = liquid });
			root.AddChild(new MeshInstance3D { Mesh = shapeMeshes[k], MaterialOverride = Glass(c, 0.22f) });
			var body = shapes[k][3];
			var lab = new ShaderMaterial { Shader = labelShader };
			lab.SetShaderParameter("seed", rng.Randf() * 10f);
			lab.SetShaderParameter("accent", accents[rng.RandiRange(0, accents.Length - 1)]);
			root.AddChild(new MeshInstance3D
			{
				Mesh = new CylinderMesh { TopRadius = body.X + 0.0012f, BottomRadius = body.X + 0.0012f, Height = body.Y * 0.5f, CapTop = false, CapBottom = false },
				Position = new Vector3(0, body.Y * 0.48f, 0),
				MaterialOverride = lab,
			});
			var top = shapes[k][^2];
			root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = top.X * 0.95f, BottomRadius = top.X * 0.8f, Height = 0.022f }, Position = new Vector3(0, top.Y + 0.006f, 0), MaterialOverride = cork });
		}

		// a cluster on the counter
		Bottle(new Vector3(0.62f, CounterTop, -0.16f), 1.0f, 0, 0, LabelYaw);
		Bottle(new Vector3(0.72f, CounterTop, -0.04f), 1.0f, 1, 1, LabelYaw);
		Bottle(new Vector3(0.82f, CounterTop, -0.2f), 1.0f, 3, 0, LabelYaw);
		Bottle(new Vector3(0.52f, CounterTop, -0.02f), 1.0f, 2, 2, LabelYaw);

		// the dresser shelves, crowded with product
		for (var shelf = 0; shelf < 2; shelf++)
			for (var i = 0; i < 14; i++)
			{
				var z = Mathf.Lerp(-1.6f, 0.2f, i / 13f) + rng.RandfRange(-0.015f, 0.015f);
				Bottle(new Vector3(-2.05f + rng.RandfRange(-0.03f, 0.03f), 1.36f + shelf * 0.42f, z), rng.RandfRange(0.9f, 1.15f), rng.RandiRange(0, 4), -1, LabelYaw - 90f);
			}
	}

	private void BuildTincture(Vector3 at)
	{
		var root = new Node3D { Position = at };
		AddChild(root);
		const float h = 0.13f, r = 0.042f;
		root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = r, BottomRadius = r * 0.85f, Height = h, Rings = 1 }, Position = new Vector3(0, h / 2, 0), MaterialOverride = Glass(new Color(0.9f, 0.95f, 1f), 0.06f) });
		var water = Glass(new Color(0.55f, 0.65f, 0.7f), 0.05f);
		root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = r * 0.94f, BottomRadius = r * 0.8f, Height = h * 0.82f }, Position = new Vector3(0, h * 0.42f, 0), MaterialOverride = water });
		var shader = GD.Load<Shader>("res://shaders/tincture.gdshader");
		for (var i = 0; i < 7; i++)
		{
			var shell = new ShaderMaterial { Shader = shader };
			shell.SetShaderParameter("seed", i * 0.37f);
			shell.SetShaderParameter("density", 0.42f);
			shell.SetShaderParameter("height", h * 0.8f);
			var k = 0.9f - i * 0.12f;
			root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = r * k, BottomRadius = r * 0.8f * k, Height = h * 0.8f }, Position = new Vector3(0, h * 0.41f, 0), MaterialOverride = shell });
		}
	}

	private void BuildCustomer(Vector3 at)
	{
		// Placeholder: a man in a frock coat and bowler, meant to read only as a silhouette
		// against the street. A real character model replaces this.
		var cloth = new StandardMaterial3D { AlbedoColor = new Color(0.006f, 0.006f, 0.007f), Roughness = 1f, MetallicSpecular = 0.1f };
		var skin = new StandardMaterial3D { AlbedoColor = new Color(0.03f, 0.022f, 0.02f), Roughness = 0.9f };
		var root = new Node3D { Position = at, RotationDegrees = new Vector3(0, 12, 0) };
		AddChild(root);
		var coat = Lathe(V(0f,0f, 0.2f,0f, 0.24f,0.35f, 0.2f,0.85f, 0.17f,1.02f, 0.2f,1.25f, 0.215f,1.36f, 0.17f,1.43f, 0.08f,1.47f, 0.055f,1.5f, 0.05f,1.55f, 0f,1.55f));
		root.AddChild(new MeshInstance3D { Mesh = coat, Scale = new Vector3(1.3f, 1f, 0.72f), MaterialOverride = cloth });
		root.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.095f, Height = 0.23f }, Position = new Vector3(0, 1.63f, 0.01f), MaterialOverride = cloth });
		var bowler = Lathe(V(0f,0f, 0.165f,0f, 0.17f,0.01f, 0.11f,0.015f, 0.105f,0.07f, 0.09f,0.11f, 0.05f,0.13f, 0f,0.135f));
		root.AddChild(new MeshInstance3D { Mesh = bowler, Position = new Vector3(0, 1.71f, 0), Scale = new Vector3(1f, 1f, 1.1f), RotationDegrees = new Vector3(-6, 0, 0), MaterialOverride = cloth });
	}

	private void BuildLights()
	{
		// the oil lamp on the counter is the key light
		AddChild(new OmniLight3D
		{
			Position = new Vector3(-0.85f, CounterTop + 0.42f, -0.05f),
			LightColor = new Color(1f, 0.6f, 0.28f),
			LightEnergy = 3.2f,
			OmniRange = 5.5f,
			OmniAttenuation = 1.4f,
			ShadowEnabled = true,
			LightVolumetricFogEnergy = 0.6f,
		});
		var flame = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1f, 0.75f, 0.4f), EmissionEnabled = true, Emission = new Color(1f, 0.6f, 0.25f), EmissionEnergyMultiplier = 6f };
		AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.012f, Height = 0.05f }, Position = new Vector3(-0.85f, CounterTop + 0.42f, -0.05f), MaterialOverride = flame });

		// gas lamp in the street: cold blue spill through the window, shafts in the fog
		var spot = new SpotLight3D
		{
			Position = new Vector3(0.2f, 3.4f, -5.4f),
			LightColor = new Color(0.5f, 0.62f, 1f),
			LightEnergy = 9f,
			SpotRange = 9f,
			SpotAngle = 32f,
			ShadowEnabled = true,
			LightVolumetricFogEnergy = 1.2f,
		};
		AddChild(spot);
		spot.LookAt(new Vector3(-0.4f, 0.6f, -0.6f));

		// cold rim from the street on the customer's shoulders and hat
		var rim = new SpotLight3D { Position = new Vector3(-0.35f, 2.5f, -3.1f), LightColor = new Color(0.55f, 0.68f, 1f), LightEnergy = 4f, SpotRange = 4f, SpotAngle = 22f, LightVolumetricFogEnergy = 0.4f };
		AddChild(rim);
		rim.LookAt(new Vector3(-0.6f, 1.45f, -0.95f));

		// faint warm fill from the back room so the dresser reads
		AddChild(new OmniLight3D { Position = new Vector3(-1.2f, 2.4f, 1.5f), LightColor = new Color(1f, 0.7f, 0.45f), LightEnergy = 0.35f, OmniRange = 4f, LightVolumetricFogEnergy = 0f });
	}

	private void BuildCamera()
	{
		// --cam wide (default): the proprietor's view across the counter.
		// --cam glass: the close-up used when a dose goes into the water.
		var args = OS.GetCmdlineUserArgs();
		var shot = "wide";
		for (var i = 0; i < args.Length - 1; i++) if (args[i] == "--cam") shot = args[i + 1];

		var close = shot == "glass";
		var attrs = new CameraAttributesPractical
		{
			DofBlurFarEnabled = true,
			DofBlurFarDistance = close ? 0.7f : 2.4f,
			DofBlurFarTransition = close ? 1.2f : 2.5f,
			DofBlurNearEnabled = !close,
			DofBlurNearDistance = 0.35f,
			DofBlurNearTransition = 0.2f,
			DofBlurAmount = 0.06f,
		};
		var cam = close
			? new Camera3D { Fov = 32f, Position = Glass0 + new Vector3(0.16f, 0.12f, 0.3f), Attributes = attrs, Current = true }
			: new Camera3D { Fov = 48f, Position = new Vector3(0.55f, 1.4f, 0.95f), Attributes = attrs, Current = true };
		AddChild(cam);
		cam.LookAt(close ? Glass0 + new Vector3(0f, 0.06f, 0f) : new Vector3(-0.35f, 1.12f, -1.2f));
	}

	private void BuildGrade()
	{
		var layer = new CanvasLayer();
		var rect = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/grade.gdshader") } };
		rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		layer.AddChild(rect);
		AddChild(layer);
	}
}
