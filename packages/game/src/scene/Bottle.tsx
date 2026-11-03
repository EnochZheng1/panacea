import type { ThreeEvent } from '@react-three/fiber';
import { useMemo, useState } from 'react';
import * as THREE from 'three';
import { asGlass, asLiquid, liquidMaterial, MAT } from './materials.ts';

export type BottleShape = 'round' | 'tall' | 'squat' | 'flask' | 'square';

/** Lathe profiles (radius, height) for each bottle shape, in metres. */
const PROFILES: Record<BottleShape, [number, number][]> = {
  round: [
    [0, 0],
    [0.045, 0],
    [0.05, 0.02],
    [0.05, 0.11],
    [0.03, 0.14],
    [0.014, 0.16],
    [0.014, 0.19],
    [0, 0.19],
  ],
  tall: [
    [0, 0],
    [0.035, 0],
    [0.038, 0.02],
    [0.038, 0.19],
    [0.022, 0.22],
    [0.012, 0.24],
    [0.012, 0.27],
    [0, 0.27],
  ],
  squat: [
    [0, 0],
    [0.06, 0],
    [0.065, 0.02],
    [0.065, 0.08],
    [0.04, 0.1],
    [0.02, 0.11],
    [0.02, 0.13],
    [0, 0.13],
  ],
  flask: [
    [0, 0],
    [0.03, 0],
    [0.065, 0.06],
    [0.06, 0.1],
    [0.025, 0.14],
    [0.014, 0.18],
    [0.014, 0.21],
    [0, 0.21],
  ],
  square: [
    [0, 0],
    [0.042, 0],
    [0.042, 0.15],
    [0.02, 0.17],
    [0.013, 0.18],
    [0.013, 0.2],
    [0, 0.2],
  ],
};

const geometryCache = new Map<string, THREE.BufferGeometry>();

function profileGeometry(shape: BottleShape, inset: number, cutAt = 1): THREE.BufferGeometry {
  const key = `${shape}:${inset}:${cutAt}`;
  const hit = geometryCache.get(key);
  if (hit) return hit;
  const profile = PROFILES[shape];
  const top = profile.at(-1)![1];
  const points = profile
    .filter(([, y]) => y <= top * cutAt)
    .map(([r, y]) => new THREE.Vector2(Math.max(0, r - inset), y));
  const last = points.at(-1)!;
  if (last.x > 0) points.push(new THREE.Vector2(0, last.y));
  const segments = shape === 'square' ? 4 : 20;
  const g = new THREE.LatheGeometry(points, segments, shape === 'square' ? Math.PI / 4 : 0);
  geometryCache.set(key, g);
  return g;
}

export const bottleHeight = (shape: BottleShape) => PROFILES[shape].at(-1)![1];

export interface BottleProps {
  shape: BottleShape;
  color: string;
  /** 0..1 */
  fill: number;
  label?: THREE.Texture;
  position?: [number, number, number];
  rotation?: [number, number, number];
  scale?: number;
  onClick?: (e: ThreeEvent<MouseEvent>) => void;
  onHover?: (hovering: boolean) => void;
}

/** A glass bottle with glowing liquid, a cork and a paper label. */
export function Bottle({
  shape,
  color,
  fill,
  label,
  position,
  rotation,
  scale = 1,
  onClick,
  onHover,
}: BottleProps) {
  const [hover, setHover] = useState(false);
  const glassGeo = profileGeometry(shape, 0);
  const liquidGeo = useMemo(() => profileGeometry(shape, 0.004, 0.62), [shape]);
  const h = bottleHeight(shape);
  const neck = PROFILES[shape].at(-2)![0];
  const bodyRadius = Math.max(...PROFILES[shape].map(([r]) => r));
  const interactive = Boolean(onClick);

  return (
    <group
      position={position}
      rotation={rotation}
      scale={scale * (hover ? 1.08 : 1)}
      onClick={onClick}
      onPointerOver={
        interactive
          ? (e) => {
              e.stopPropagation();
              setHover(true);
              onHover?.(true);
              document.body.style.cursor = 'pointer';
            }
          : undefined
      }
      onPointerOut={
        interactive
          ? () => {
              setHover(false);
              onHover?.(false);
              document.body.style.cursor = '';
            }
          : undefined
      }
    >
      <mesh ref={asGlass} geometry={glassGeo} material={MAT.glass} />
      {fill > 0.01 && (
        <mesh
          ref={asLiquid}
          geometry={liquidGeo}
          material={liquidMaterial(color)}
          scale={[1, Math.min(1, fill), 1]}
          position={[0, 0.003, 0]}
        />
      )}
      <mesh position={[0, h + 0.012, 0]} material={MAT.wood} castShadow>
        <cylinderGeometry args={[neck * 1.05, neck * 0.9, 0.03, 10]} />
      </mesh>
      {label && (
        <mesh position={[0, h * 0.38, bodyRadius + 0.002]}>
          <planeGeometry args={[bodyRadius * 1.9, bodyRadius * 1.35]} />
          <meshStandardMaterial map={label} roughness={1} />
        </mesh>
      )}
    </group>
  );
}
