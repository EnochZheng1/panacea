import { useFrame, useThree } from '@react-three/fiber';
import { useEffect, useRef } from 'react';
import * as THREE from 'three';

export type Station = 'overview' | 'bench' | 'still' | 'counter' | 'desk';

/** Where the camera stands at each station, and what it looks at. */
export const STATIONS: Record<
  Station,
  { position: [number, number, number]; target: [number, number, number]; fov: number }
> = {
  overview: { position: [1.4, 2.95, 3.05], target: [-0.1, 0.85, -1.5], fov: 64 },
  bench: { position: [-1.3, 1.62, -0.25], target: [-1.3, 1.25, -2.6], fov: 50 },
  still: { position: [1.35, 1.7, 0.35], target: [3.05, 1.05, -1.6], fov: 52 },
  counter: { position: [0.35, 1.65, -0.45], target: [0.35, 1.2, 2.4], fov: 55 },
  desk: { position: [-1.85, 1.65, 0.7], target: [-3.4, 0.85, 0.65], fov: 48 },
};

/**
 * Glides between stations, with a slight handheld sway and a gentle parallax toward the pointer
 * so the diorama never feels like a still image. Respects reduced motion.
 */
export function CameraRig({
  station,
  reducedMotion,
}: {
  station: Station;
  reducedMotion: boolean;
}) {
  const { camera, pointer } = useThree();
  const target = useRef(new THREE.Vector3(...STATIONS.overview.target));
  const desired = useRef(new THREE.Vector3());
  const desiredTarget = useRef(new THREE.Vector3());

  useEffect(() => {
    if (reducedMotion) {
      const s = STATIONS[station];
      camera.position.set(...s.position);
      target.current.set(...s.target);
    }
  }, [station, reducedMotion, camera]);

  useFrame(({ clock }, delta) => {
    const s = STATIONS[station];
    const t = clock.elapsedTime;
    const sway = reducedMotion ? 0 : 1;
    desired.current.set(
      s.position[0] + (pointer.x * 0.08 + Math.sin(t * 0.3) * 0.015) * sway,
      s.position[1] + (pointer.y * 0.05 + Math.sin(t * 0.23) * 0.01) * sway,
      s.position[2],
    );
    desiredTarget.current.set(...s.target);
    const k = reducedMotion ? 1 : 1 - Math.exp(-delta * 2.6);
    camera.position.lerp(desired.current, k);
    target.current.lerp(desiredTarget.current, k);
    const cam = camera as THREE.PerspectiveCamera;
    cam.fov += (s.fov - cam.fov) * k;
    cam.updateProjectionMatrix();
    camera.lookAt(target.current);
  });
  return null;
}
