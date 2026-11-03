import { useFrame, type ThreeEvent } from '@react-three/fiber';
import { useMemo, useRef } from 'react';
import * as THREE from 'three';
import { asGlass, asLiquid, liquidMaterial, MAT } from './materials.ts';
import { dialTexture } from './textures.ts';

export const STILL = { x: 2.6, z: -2.0 } as const;

export interface StillProps {
  /** 0..1 target pressure; the needle and steam follow it. */
  pressure: number;
  /** Colour of what is being distilled (or last distilled). */
  color: string;
  /** 0..1 how full the receiving flask is. */
  received: number;
  onClick?: (e: ThreeEvent<MouseEvent>) => void;
}

const potProfile = [
  [0, 0],
  [0.3, 0],
  [0.38, 0.08],
  [0.4, 0.3],
  [0.36, 0.46],
  [0.24, 0.56],
  [0.12, 0.6],
  [0.1, 0.66],
  [0, 0.66],
].map(([r, y]) => new THREE.Vector2(r!, y!));

/** A brass-and-copper pot still on a brick hearth, with a swan neck, worm condenser and receiver. */
export function Still({ pressure, color, received, onClick }: StillProps) {
  const needle = useRef<THREE.Group>(null);
  const wheel = useRef<THREE.Group>(null);
  const fire = useRef<THREE.PointLight>(null);
  const steam = useRef<THREE.Group>(null);
  const shown = useRef(0);

  const potGeo = useMemo(() => new THREE.LatheGeometry(potProfile, 28), []);
  const neckCurve = useMemo(
    () =>
      new THREE.CatmullRomCurve3([
        new THREE.Vector3(0, 1.62, 0),
        new THREE.Vector3(0.05, 1.95, 0.02),
        new THREE.Vector3(0.35, 2.1, 0.15),
        new THREE.Vector3(0.8, 1.75, 0.55),
        new THREE.Vector3(1.04, 1.36, 0.75),
      ]),
    [],
  );
  const coilCurve = useMemo(() => {
    const pts: THREE.Vector3[] = [];
    for (let i = 0; i <= 120; i++) {
      const t = i / 120;
      const a = t * Math.PI * 2 * 5;
      pts.push(
        new THREE.Vector3(0.9 + Math.cos(a) * 0.16, 1.27 - t * 0.6, 0.75 + Math.sin(a) * 0.16),
      );
    }
    return new THREE.CatmullRomCurve3(pts);
  }, []);

  useFrame(({ clock }, delta) => {
    shown.current += (pressure - shown.current) * Math.min(1, delta * 1.5);
    const p = shown.current;
    const t = clock.elapsedTime;
    if (needle.current) {
      // Dial sweeps 270°; the needle trembles as pressure rises.
      const tremble = p > 0.05 ? Math.sin(t * 31) * 0.02 * (0.3 + p) : 0;
      needle.current.rotation.z = Math.PI * 0.75 - Math.PI * 1.5 * p - tremble;
    }
    if (wheel.current) wheel.current.rotation.z = -p * Math.PI * 3;
    if (fire.current)
      fire.current.intensity = 0.4 + p * 5 * (0.85 + 0.15 * Math.sin(t * 9) * Math.sin(t * 5.3));
    if (steam.current) {
      steam.current.children.forEach((puff, i) => {
        const phase = (t * 0.5 + i / steam.current!.children.length) % 1;
        puff.position.set(
          Math.sin(i * 2.1 + t) * 0.08,
          1.52 + phase * 0.9,
          Math.cos(i * 1.7) * 0.08,
        );
        const s = (0.05 + phase * 0.18) * Math.min(1, p * 2.5);
        puff.scale.setScalar(Math.max(0.0001, s));
        puff.visible = p > 0.15;
      });
    }
  });

  const hearthTop = 0.62;
  return (
    <group position={[STILL.x, 0, STILL.z]} onClick={onClick}>
      {/* Brick hearth with a firebox */}
      <mesh position={[0, hearthTop / 2, 0]} material={MAT.brick} castShadow receiveShadow>
        <boxGeometry args={[1.1, hearthTop, 1.0]} />
      </mesh>
      <mesh position={[0, 0.25, 0.501]} material={MAT.iron}>
        <planeGeometry args={[0.42, 0.3]} />
      </mesh>
      <pointLight
        ref={fire}
        position={[0, 0.3, 0.7]}
        color="#ff7a2a"
        intensity={0.5}
        distance={3}
        decay={2}
      />

      {/* Pot and dome */}
      <mesh
        geometry={potGeo}
        position={[0, hearthTop, 0]}
        material={MAT.copper}
        castShadow
        receiveShadow
      />
      <mesh position={[0, hearthTop + 0.66 + 0.15, 0]} material={MAT.brass} castShadow>
        <sphereGeometry args={[0.17, 20, 14, 0, Math.PI * 2, 0, Math.PI / 2]} />
      </mesh>
      <mesh position={[0, hearthTop + 0.7, 0]} material={MAT.brass}>
        <cylinderGeometry args={[0.13, 0.13, 0.14, 20]} />
      </mesh>
      {/* Rivet band */}
      <mesh position={[0, hearthTop + 0.3, 0]} rotation={[Math.PI / 2, 0, 0]} material={MAT.brass}>
        <torusGeometry args={[0.402, 0.012, 6, 40]} />
      </mesh>

      {/* Swan neck → worm condenser in a tub → receiving flask */}
      <mesh material={MAT.copper} castShadow>
        <tubeGeometry args={[neckCurve, 40, 0.035, 10, false]} />
      </mesh>
      <mesh material={MAT.copper} castShadow>
        <tubeGeometry args={[coilCurve, 300, 0.018, 8, false]} />
      </mesh>
      <mesh position={[0.9, 1.05, 0.75]} material={MAT.wood} castShadow>
        <cylinderGeometry args={[0.24, 0.26, 0.5, 20, 1, true]} />
      </mesh>
      {[0.85, 1.25].map((y) => (
        <mesh key={y} position={[0.9, y, 0.75]} rotation={[Math.PI / 2, 0, 0]} material={MAT.iron}>
          <torusGeometry args={[0.25, 0.012, 6, 30]} />
        </mesh>
      ))}
      <mesh position={[0.9, 0.38, 0.75]} material={MAT.darkWood} castShadow>
        <cylinderGeometry args={[0.28, 0.28, 0.76, 16]} />
      </mesh>
      <group position={[1.25, 0.76, 0.85]}>
        <mesh ref={asGlass} material={MAT.glass} position={[0, 0.1, 0]}>
          <sphereGeometry args={[0.1, 16, 12]} />
        </mesh>
        <mesh ref={asGlass} material={MAT.glass} position={[0, 0.22, 0]}>
          <cylinderGeometry args={[0.025, 0.03, 0.1, 10, 1, true]} />
        </mesh>
        {received > 0.02 && (
          <mesh
            ref={asLiquid}
            material={liquidMaterial(color)}
            position={[0, 0.03 + received * 0.06, 0]}
            scale={[1, Math.max(0.15, received), 1]}
          >
            <sphereGeometry args={[0.092, 16, 10, 0, Math.PI * 2, Math.PI / 2, Math.PI / 2]} />
          </mesh>
        )}
      </group>
      <mesh position={[1.2, 0.72, 0.8]} rotation={[0, 0, 0.9]} material={MAT.copper}>
        <cylinderGeometry args={[0.012, 0.012, 0.18, 6]} />
      </mesh>

      {/* Pressure gauge on the pot, facing the room */}
      <group position={[0.18, hearthTop + 0.5, 0.36]} rotation={[0, 0.45, 0]}>
        <mesh material={MAT.brass} rotation={[Math.PI / 2, 0, 0]}>
          <cylinderGeometry args={[0.11, 0.11, 0.05, 28]} />
        </mesh>
        <mesh position={[0, 0, 0.026]}>
          <circleGeometry args={[0.095, 32]} />
          <meshStandardMaterial map={dialTexture()} roughness={0.8} />
        </mesh>
        <group ref={needle} position={[0, 0, 0.03]}>
          <mesh position={[0, 0.04, 0]} material={MAT.iron}>
            <boxGeometry args={[0.006, 0.08, 0.004]} />
          </mesh>
        </group>
      </group>

      {/* Steam valve wheel */}
      <group ref={wheel} position={[-0.3, hearthTop + 0.85, 0.25]} rotation={[0, 0.3, 0]}>
        <mesh material={MAT.iron}>
          <torusGeometry args={[0.08, 0.012, 8, 24]} />
        </mesh>
        {[0, 1, 2].map((i) => (
          <mesh key={i} rotation={[0, 0, (i * Math.PI) / 3]} material={MAT.iron}>
            <boxGeometry args={[0.16, 0.01, 0.01]} />
          </mesh>
        ))}
      </group>

      {/* Steam puffs (rendered as pale plaster blobs; the ink pass hatches them softly) */}
      <group ref={steam}>
        {Array.from({ length: 6 }, (_, i) => (
          <mesh key={i} material={MAT.paper}>
            <sphereGeometry args={[1, 10, 8]} />
          </mesh>
        ))}
      </group>
    </group>
  );
}
