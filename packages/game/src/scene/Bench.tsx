import { useFrame } from '@react-three/fiber';
import { useMemo, useRef } from 'react';
import * as THREE from 'three';
import { asGlass, asLiquid, liquidMaterial, MAT } from './materials.ts';

export const BENCH = { x: -1.3, z: -1.75, top: 0.92 } as const;
const GLASS = { x: BENCH.x, z: BENCH.z + 0.05, radius: 0.075, height: 0.3 } as const;

export interface BenchProps {
  /** Fluid ounces in the measuring glass, and the bottle's capacity. */
  volume: number;
  capacity: number;
  color: string;
  /** Changes every time something is poured; drives the pouring animation. */
  pour: { key: number; color: string } | null;
}

/** The compounding bench: a graduated measuring glass, mortar and pestle, apothecary scales. */
export function Bench({ volume, capacity, color, pour }: BenchProps) {
  const level = Math.min(1, volume / capacity);
  const liquid = useRef<THREE.Mesh>(null);
  const stream = useRef<THREE.Mesh>(null);
  const pourStart = useRef<{ key: number; at: number }>({ key: -1, at: -10 });
  const shown = useRef(level);

  useFrame(({ clock }, delta) => {
    // Liquid level eases toward its target, so pouring visibly fills the glass.
    shown.current += (level - shown.current) * Math.min(1, delta * 4);
    if (liquid.current) {
      const h = Math.max(0.001, shown.current * GLASS.height * 0.92);
      liquid.current.scale.y = h;
      liquid.current.position.y = BENCH.top + 0.01 + h / 2;
      liquid.current.visible = shown.current > 0.005;
    }
    if (pour && pour.key !== pourStart.current.key)
      pourStart.current = { key: pour.key, at: clock.elapsedTime };
    if (stream.current) {
      const t = clock.elapsedTime - pourStart.current.at;
      const on = t < 0.8;
      stream.current.visible = on;
      if (on) {
        const wobble = 1 + 0.15 * Math.sin(t * 40);
        stream.current.scale.set(wobble, 1, wobble);
      }
    }
  });

  const graduations = useMemo(
    () =>
      Array.from({ length: 8 }, (_, i) => BENCH.top + 0.02 + ((i + 1) / 8) * GLASS.height * 0.92),
    [],
  );

  return (
    <group>
      {/* Table */}
      <mesh
        position={[BENCH.x, BENCH.top - 0.03, BENCH.z]}
        material={MAT.wood}
        castShadow
        receiveShadow
      >
        <boxGeometry args={[2.2, 0.06, 0.85]} />
      </mesh>
      {[
        [-1, -0.36],
        [1, -0.36],
        [-1, 0.36],
        [1, 0.36],
      ].map(([dx, dz]) => (
        <mesh
          key={`${dx}${dz}`}
          position={[BENCH.x + dx!, (BENCH.top - 0.06) / 2, BENCH.z + dz!]}
          material={MAT.darkWood}
          castShadow
        >
          <boxGeometry args={[0.07, BENCH.top - 0.06, 0.07]} />
        </mesh>
      ))}

      {/* Measuring glass */}
      <mesh
        ref={asGlass}
        position={[GLASS.x, BENCH.top + GLASS.height / 2 + 0.005, GLASS.z]}
        material={MAT.glass}
      >
        <cylinderGeometry args={[GLASS.radius, GLASS.radius * 0.92, GLASS.height, 24, 1, true]} />
      </mesh>
      <mesh position={[GLASS.x, BENCH.top + 0.006, GLASS.z]} material={MAT.glass} ref={asGlass}>
        <cylinderGeometry args={[GLASS.radius * 1.15, GLASS.radius * 1.2, 0.012, 24]} />
      </mesh>
      {graduations.map((y) => (
        <mesh key={y} position={[GLASS.x, y, GLASS.z + GLASS.radius * 0.97]} material={MAT.iron}>
          <boxGeometry args={[0.03, 0.002, 0.002]} />
        </mesh>
      ))}
      <mesh
        ref={(m) => {
          asLiquid(m);
          liquid.current = m;
        }}
        position={[GLASS.x, BENCH.top, GLASS.z]}
        material={liquidMaterial(color)}
      >
        <cylinderGeometry args={[GLASS.radius * 0.93, GLASS.radius * 0.86, 1, 24]} />
      </mesh>
      <mesh
        ref={(m) => {
          asLiquid(m);
          stream.current = m;
        }}
        position={[GLASS.x + 0.01, BENCH.top + GLASS.height + 0.2, GLASS.z]}
        material={liquidMaterial(pour?.color ?? color)}
        visible={false}
      >
        <cylinderGeometry args={[0.006, 0.009, 0.5, 8]} />
      </mesh>

      {/* Mortar and pestle */}
      <group position={[BENCH.x - 0.6, BENCH.top, BENCH.z + 0.1]}>
        <mesh material={MAT.plaster} castShadow position={[0, 0.06, 0]}>
          <cylinderGeometry args={[0.1, 0.07, 0.12, 18, 1]} />
        </mesh>
        <mesh material={MAT.plaster} castShadow position={[0.03, 0.15, 0]} rotation={[0, 0, -0.5]}>
          <cylinderGeometry args={[0.018, 0.03, 0.22, 10]} />
        </mesh>
      </group>

      {/* Apothecary scales */}
      <group position={[BENCH.x + 0.65, BENCH.top, BENCH.z - 0.1]}>
        <mesh material={MAT.darkWood} position={[0, 0.02, 0]} castShadow>
          <boxGeometry args={[0.34, 0.04, 0.16]} />
        </mesh>
        <mesh material={MAT.brass} position={[0, 0.2, 0]} castShadow>
          <cylinderGeometry args={[0.008, 0.008, 0.34, 6]} />
        </mesh>
        <mesh material={MAT.brass} position={[0, 0.36, 0]} rotation={[0, 0, 0.06]}>
          <boxGeometry args={[0.36, 0.01, 0.01]} />
        </mesh>
        {[-0.17, 0.17].map((x, i) => (
          <mesh
            key={x}
            material={MAT.brass}
            position={[x, 0.24 + (i ? -0.02 : 0.02), 0]}
            castShadow
          >
            <cylinderGeometry args={[0.06, 0.04, 0.015, 16]} />
          </mesh>
        ))}
      </group>

      {/* A few loose bottles and papers for life */}
      <mesh
        position={[BENCH.x + 0.25, BENCH.top + 0.002, BENCH.z + 0.25]}
        rotation={[-Math.PI / 2, 0, 0.3]}
        material={MAT.paper}
      >
        <planeGeometry args={[0.22, 0.3]} />
      </mesh>
    </group>
  );
}
