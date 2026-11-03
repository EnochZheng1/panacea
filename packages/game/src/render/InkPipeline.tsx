import { useFrame, useThree } from '@react-three/fiber';
import { useEffect, useMemo } from 'react';
import * as THREE from 'three';
import { inkFragmentShader, inkVertexShader } from './inkShader.ts';

/**
 * Takes over rendering (a priority-1 frame callback) to produce the engraved look:
 *   1. the scene in colour (with a depth texture),
 *   2. view-space normals (for ink outlines),
 *   3. a liquid mask: liquids in their own colour, everything else black, glass hidden,
 *   4. the ink composite on a fullscreen triangle.
 *
 * Meshes opt in with `userData.liquid = true` (glowing colour) or `userData.glass = true`
 * (ignored by the mask so liquids show through their bottles).
 */
export function InkPipeline({ enabled }: { enabled: boolean }) {
  const { gl, size, scene, camera, viewport } = useThree();

  const targets = useMemo(() => {
    const make = (withDepth: boolean) => {
      const rt = new THREE.WebGLRenderTarget(1, 1, {
        type: THREE.HalfFloatType,
        samples: 0,
      });
      if (withDepth) rt.depthTexture = new THREE.DepthTexture(1, 1, THREE.UnsignedIntType);
      return rt;
    };
    return { color: make(true), normal: make(false), mask: make(false) };
  }, []);

  const materials = useMemo(
    () => ({
      normal: new THREE.MeshNormalMaterial(),
      black: new THREE.MeshBasicMaterial({ color: 0x000000 }),
    }),
    [],
  );

  const composite = useMemo(() => {
    const material = new THREE.ShaderMaterial({
      vertexShader: inkVertexShader,
      fragmentShader: inkFragmentShader,
      depthTest: false,
      depthWrite: false,
      uniforms: {
        tColor: { value: null },
        tDepth: { value: null },
        tNormal: { value: null },
        tMask: { value: null },
        resolution: { value: new THREE.Vector2(1, 1) },
        pixelRatio: { value: 1 },
        cameraNear: { value: 0.1 },
        cameraFar: { value: 50 },
        time: { value: 0 },
        inkEnabled: { value: 1 },
        paperColor: { value: new THREE.Color('#efe3c8') },
        inkColor: { value: new THREE.Color('#2a1a10') },
      },
    });
    // A single triangle covering the screen.
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute(
      'position',
      new THREE.Float32BufferAttribute([-1, -1, 0, 3, -1, 0, -1, 3, 0], 3),
    );
    geometry.setAttribute('uv', new THREE.Float32BufferAttribute([0, 0, 2, 0, 0, 2], 2));
    const mesh = new THREE.Mesh(geometry, material);
    mesh.frustumCulled = false;
    const quadScene = new THREE.Scene();
    quadScene.add(mesh);
    return { material, quadScene, quadCamera: new THREE.OrthographicCamera(-1, 1, 1, -1, 0, 1) };
  }, []);

  useEffect(() => {
    const dpr = viewport.dpr;
    const w = Math.max(1, Math.floor(size.width * dpr));
    const h = Math.max(1, Math.floor(size.height * dpr));
    for (const rt of Object.values(targets)) rt.setSize(w, h);
    composite.material.uniforms['resolution']!.value.set(w, h);
    composite.material.uniforms['pixelRatio']!.value = dpr;
  }, [size, viewport.dpr, targets, composite]);

  useEffect(
    () => () => {
      for (const rt of Object.values(targets)) rt.dispose();
      materials.normal.dispose();
      materials.black.dispose();
      composite.material.dispose();
    },
    [targets, materials, composite],
  );

  useFrame((state) => {
    const u = composite.material.uniforms;
    const cam = camera as THREE.PerspectiveCamera;
    u['time']!.value = state.clock.elapsedTime;
    u['cameraNear']!.value = cam.near;
    u['cameraFar']!.value = cam.far;
    u['inkEnabled']!.value = enabled ? 1 : 0;

    const background = scene.background;

    // 1. Colour + depth.
    gl.setRenderTarget(targets.color);
    gl.render(scene, camera);

    // 2. Normals (skipped when the ink effect is off).
    if (enabled) {
      scene.background = null;
      scene.overrideMaterial = materials.normal;
      gl.setRenderTarget(targets.normal);
      gl.setClearColor(0x8080ff, 1);
      gl.clear();
      gl.render(scene, camera);
      scene.overrideMaterial = null;
    }

    // 3. Liquid mask.
    const swapped: [THREE.Mesh, THREE.Material | THREE.Material[]][] = [];
    const hidden: THREE.Object3D[] = [];
    scene.traverse((obj) => {
      const mesh = obj as THREE.Mesh;
      if (!mesh.isMesh) return;
      if (obj.userData['glass'] && obj.visible) {
        obj.visible = false;
        hidden.push(obj);
      } else if (!obj.userData['liquid']) {
        swapped.push([mesh, mesh.material]);
        mesh.material = materials.black;
      }
    });
    scene.background = null;
    gl.setRenderTarget(targets.mask);
    gl.setClearColor(0x000000, 1);
    gl.clear();
    gl.render(scene, camera);
    for (const [mesh, material] of swapped) mesh.material = material;
    for (const obj of hidden) obj.visible = true;
    scene.background = background;

    // 4. Composite to the screen.
    u['tColor']!.value = targets.color.texture;
    u['tDepth']!.value = targets.color.depthTexture;
    u['tNormal']!.value = targets.normal.texture;
    u['tMask']!.value = targets.mask.texture;
    gl.setRenderTarget(null);
    gl.render(composite.quadScene, composite.quadCamera);
  }, 1);

  return null;
}
