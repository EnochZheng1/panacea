import type { ThreeEvent } from '@react-three/fiber';
import { useFrame } from '@react-three/fiber';
import { useRef } from 'react';
import * as THREE from 'three';
import { asGlass, MAT } from './materials.ts';
import { calendarTexture, ledgerTexture } from './textures.ts';

export const DESK = { x: -3.3, z: 0.7, top: 0.78 } as const;

export interface DeskProps {
  month: string;
  year: number;
  letters: number;
  ledgerLines: string[];
  /** Rung to close the month. */
  onBell?: (e: ThreeEvent<MouseEvent>) => void;
}

/** The writing desk by the wall: the Ledger, the morning's letters, the calendar, the bell. */
export function Desk({ month, year, letters, ledgerLines, onBell }: DeskProps) {
  const bell = useRef<THREE.Group>(null);
  const rung = useRef(-10);
  useFrame(({ clock }) => {
    if (!bell.current) return;
    const t = clock.elapsedTime - rung.current;
    bell.current.rotation.z = t < 1 ? Math.sin(t * 30) * 0.25 * (1 - t) : 0;
  });

  return (
    <group position={[DESK.x, 0, DESK.z]} rotation={[0, Math.PI / 2, 0]}>
      {/* Desk and drawers */}
      <mesh position={[0, DESK.top - 0.025, 0]} material={MAT.wood} castShadow receiveShadow>
        <boxGeometry args={[1.5, 0.05, 0.75]} />
      </mesh>
      {[-0.5, 0.5].map((dx) => (
        <mesh key={dx} position={[dx, (DESK.top - 0.05) / 2, 0]} material={MAT.darkWood} castShadow>
          <boxGeometry args={[0.45, DESK.top - 0.05, 0.7]} />
        </mesh>
      ))}
      {/* Pigeonhole hutch */}
      <mesh position={[0, DESK.top + 0.3, -0.3]} material={MAT.darkWood} castShadow>
        <boxGeometry args={[1.5, 0.6, 0.15]} />
      </mesh>

      {/* The open Ledger */}
      <group position={[-0.1, DESK.top + 0.02, 0.05]} rotation={[-Math.PI / 2, 0, 0]}>
        <mesh position={[0, 0, -0.01]} material={MAT.leather}>
          <boxGeometry args={[0.62, 0.42, 0.02]} />
        </mesh>
        <mesh position={[0, 0, 0.005]}>
          <planeGeometry args={[0.58, 0.4]} />
          <meshStandardMaterial map={ledgerTexture(ledgerLines)} roughness={1} />
        </mesh>
      </group>

      {/* Letters: one per item in The Post */}
      {Array.from({ length: Math.min(letters, 6) }, (_, i) => (
        <mesh
          key={i}
          position={[0.48, DESK.top + 0.005 + i * 0.004, 0.08 + (i % 2) * 0.02]}
          rotation={[-Math.PI / 2, 0, (i - 2) * 0.12]}
          material={MAT.paper}
          castShadow
        >
          <planeGeometry args={[0.2, 0.13]} />
        </mesh>
      ))}

      {/* Inkwell and quill */}
      <mesh position={[-0.55, DESK.top + 0.03, -0.12]} material={MAT.iron}>
        <cylinderGeometry args={[0.035, 0.04, 0.06, 12]} />
      </mesh>
      <mesh
        position={[-0.55, DESK.top + 0.12, -0.12]}
        rotation={[0.2, 0, 0.3]}
        material={MAT.paper}
      >
        <coneGeometry args={[0.012, 0.22, 6]} />
      </mesh>

      {/* Oil lamp */}
      <group position={[0.6, DESK.top, -0.18]}>
        <mesh material={MAT.brass} position={[0, 0.05, 0]} castShadow>
          <cylinderGeometry args={[0.06, 0.08, 0.1, 14]} />
        </mesh>
        <mesh ref={asGlass} material={MAT.glass} position={[0, 0.2, 0]}>
          <cylinderGeometry args={[0.035, 0.05, 0.2, 14, 1, true]} />
        </mesh>
        <pointLight
          position={[0, 0.2, 0.1]}
          color="#ffb766"
          intensity={1.6}
          distance={2.5}
          decay={2}
        />
      </group>

      {/* The bell that closes the month */}
      <group
        ref={bell}
        position={[0.25, DESK.top, 0.25]}
        onClick={(e) => {
          rung.current = e.nativeEvent.timeStamp / 1000;
          onBell?.(e);
        }}
        onPointerOver={(e) => {
          e.stopPropagation();
          document.body.style.cursor = 'pointer';
        }}
        onPointerOut={() => (document.body.style.cursor = '')}
      >
        <mesh material={MAT.brass} position={[0, 0.04, 0]} castShadow>
          <sphereGeometry args={[0.07, 18, 12, 0, Math.PI * 2, 0, Math.PI / 2]} />
        </mesh>
        <mesh material={MAT.darkWood} position={[0, 0.01, 0]}>
          <cylinderGeometry args={[0.09, 0.09, 0.02, 18]} />
        </mesh>
        <mesh material={MAT.brass} position={[0, 0.12, 0]}>
          <sphereGeometry args={[0.015, 8, 6]} />
        </mesh>
      </group>

      {/* Calendar on the wall above */}
      <mesh position={[0.45, 1.55, -0.38]}>
        <planeGeometry args={[0.32, 0.4]} />
        <meshStandardMaterial map={calendarTexture(month, year)} roughness={1} />
      </mesh>

      {/* Chair */}
      <group position={[0, 0, 0.65]}>
        <mesh material={MAT.darkWood} position={[0, 0.46, 0]} castShadow>
          <boxGeometry args={[0.45, 0.04, 0.42]} />
        </mesh>
        <mesh material={MAT.darkWood} position={[0, 0.78, 0.2]} castShadow>
          <boxGeometry args={[0.45, 0.6, 0.04]} />
        </mesh>
        {[
          [-0.2, -0.18],
          [0.2, -0.18],
          [-0.2, 0.18],
          [0.2, 0.18],
        ].map(([dx, dz]) => (
          <mesh key={`${dx}${dz}`} material={MAT.darkWood} position={[dx!, 0.22, dz!]}>
            <boxGeometry args={[0.04, 0.44, 0.04]} />
          </mesh>
        ))}
      </group>
    </group>
  );
}
