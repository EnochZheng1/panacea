import { useFrame } from '@react-three/fiber';
import { useMemo, useRef } from 'react';
import * as THREE from 'three';
import { MAT } from './materials.ts';
import { signTexture } from './textures.ts';

/**
 * The shop: 8 m × 6.4 m, back wall at z = −3, front (street) wall at z = 3.2. Floorboards,
 * wainscoting, plaster, ceiling beams, a shop window and door onto the street, gas lamps.
 */
export const ROOM = { minX: -4, maxX: 4, backZ: -3, frontZ: 3.2, height: 3.2 } as const;

function Floor() {
  const boards = useMemo(() => {
    const out: { x: number; tone: number }[] = [];
    for (let x = ROOM.minX + 0.1; x < ROOM.maxX; x += 0.2)
      out.push({ x, tone: 0.85 + ((x * 7919) % 1) * 0.3 });
    return out;
  }, []);
  const depth = ROOM.frontZ - ROOM.backZ;
  return (
    <group>
      {boards.map((b, i) => (
        <mesh
          key={i}
          position={[b.x, -0.01, (ROOM.frontZ + ROOM.backZ) / 2]}
          receiveShadow
          material={MAT.floor}
          scale={[1, 1, 1]}
        >
          <boxGeometry args={[0.19, 0.02, depth]} />
        </mesh>
      ))}
    </group>
  );
}

function Wall({
  position,
  rotation,
  width,
}: {
  position: [number, number, number];
  rotation?: [number, number, number];
  width: number;
}) {
  return (
    <group position={position} rotation={rotation}>
      <mesh position={[0, ROOM.height / 2, 0]} receiveShadow material={MAT.plaster}>
        <boxGeometry args={[width, ROOM.height, 0.1]} />
      </mesh>
      {/* Wainscot panels and dado rail */}
      <mesh position={[0, 0.5, 0.06]} receiveShadow material={MAT.wainscot}>
        <boxGeometry args={[width, 1, 0.04]} />
      </mesh>
      <mesh position={[0, 1.02, 0.09]} material={MAT.darkWood}>
        <boxGeometry args={[width, 0.05, 0.05]} />
      </mesh>
      {Array.from({ length: Math.floor(width / 0.8) }, (_, i) => (
        <mesh key={i} position={[-width / 2 + 0.4 + i * 0.8, 0.5, 0.085]} material={MAT.darkWood}>
          <boxGeometry args={[0.6, 0.7, 0.01]} />
        </mesh>
      ))}
      <mesh position={[0, ROOM.height - 0.06, 0.08]} material={MAT.darkWood}>
        <boxGeometry args={[width, 0.12, 0.06]} />
      </mesh>
    </group>
  );
}

function FrontWall() {
  // A wall with a large shop window (left) and a door (right).
  const y = ROOM.height / 2;
  const z = ROOM.frontZ;
  return (
    <group>
      <mesh position={[-3.2, y, z]} material={MAT.plaster}>
        <boxGeometry args={[1.6, ROOM.height, 0.1]} />
      </mesh>
      <mesh position={[0.2, 2.75, z]} material={MAT.plaster}>
        <boxGeometry args={[5.2, 0.9, 0.1]} />
      </mesh>
      <mesh position={[-0.6, 0.35, z]} material={MAT.wainscot}>
        <boxGeometry args={[3.6, 0.7, 0.12]} />
      </mesh>
      <mesh position={[3.35, y, z]} material={MAT.plaster}>
        <boxGeometry args={[1.3, ROOM.height, 0.1]} />
      </mesh>
      {/* Shop window: glass, mullions */}
      <mesh position={[-0.6, 1.5, z]} material={MAT.windowGlass}>
        <planeGeometry args={[3.6, 1.6]} />
      </mesh>
      {[-2.4, -1.2, 0, 1.2].map((x) => (
        <mesh key={x} position={[x, 1.5, z - 0.02]} material={MAT.darkWood}>
          <boxGeometry args={[0.06, 1.6, 0.06]} />
        </mesh>
      ))}
      <mesh position={[-0.6, 1.5, z - 0.02]} material={MAT.darkWood}>
        <boxGeometry args={[3.6, 0.05, 0.06]} />
      </mesh>
      {/* Door */}
      <mesh position={[2.05, 1.15, z - 0.02]} material={MAT.darkWood}>
        <boxGeometry args={[1.2, 2.3, 0.06]} />
      </mesh>
      <mesh position={[2.05, 1.55, z - 0.06]} material={MAT.windowGlass}>
        <planeGeometry args={[0.8, 0.9]} />
      </mesh>
      <mesh position={[1.6, 1.1, z - 0.08]} material={MAT.brass}>
        <sphereGeometry args={[0.035, 10, 8]} />
      </mesh>
    </group>
  );
}

function Beams() {
  return (
    <group>
      <mesh position={[0, ROOM.height, 0.1]} material={MAT.plaster}>
        <boxGeometry args={[8, 0.05, 6.4]} />
      </mesh>
      {[-2.5, -0.5, 1.5].map((z) => (
        <mesh key={z} position={[0, ROOM.height - 0.12, z]} material={MAT.darkWood} castShadow>
          <boxGeometry args={[8, 0.2, 0.22]} />
        </mesh>
      ))}
    </group>
  );
}

/** A gas lamp with a slow flicker. */
function GasLamp({
  position,
  intensity = 6,
  castShadow = false,
}: {
  position: [number, number, number];
  intensity?: number;
  castShadow?: boolean;
}) {
  const light = useRef<THREE.PointLight>(null);
  const seed = position[0] * 13.7 + position[2] * 3.1;
  useFrame(({ clock }) => {
    if (!light.current) return;
    const t = clock.elapsedTime + seed;
    light.current.intensity =
      intensity * (0.92 + 0.05 * Math.sin(t * 7.1) + 0.03 * Math.sin(t * 13.3));
  });
  return (
    <group position={position}>
      <pointLight
        ref={light}
        color="#ffc47a"
        intensity={intensity}
        distance={7}
        decay={1.6}
        castShadow={castShadow}
        shadow-mapSize-width={1024}
        shadow-mapSize-height={1024}
        shadow-bias={-0.002}
      />
      <mesh material={MAT.brass}>
        <cylinderGeometry args={[0.02, 0.02, 0.5, 6]} />
      </mesh>
      <mesh position={[0, -0.3, 0]} material={MAT.glass}>
        <sphereGeometry args={[0.09, 12, 10]} />
      </mesh>
    </group>
  );
}

export function Room() {
  return (
    <group>
      <Floor />
      <Wall position={[0, 0, ROOM.backZ]} width={8} />
      <Wall position={[ROOM.minX, 0, 0.1]} rotation={[0, Math.PI / 2, 0]} width={6.4} />
      <Wall position={[ROOM.maxX, 0, 0.1]} rotation={[0, -Math.PI / 2, 0]} width={6.4} />
      <FrontWall />
      <Beams />
      <mesh position={[-1.1, 2.82, ROOM.backZ + 0.08]}>
        <planeGeometry args={[2.6, 0.49]} />
        <meshStandardMaterial map={signTexture()} roughness={0.6} />
      </mesh>
      <ambientLight intensity={0.35} color="#d8c8a8" />
      <hemisphereLight args={['#ffe8c0', '#3a2616', 0.7]} />
      <GasLamp position={[-1.2, 2.75, -1.4]} intensity={12} castShadow />
      <GasLamp position={[2.4, 2.75, -1.2]} intensity={9} />
      <GasLamp position={[0.3, 2.75, 1.2]} intensity={8} />
      <GasLamp position={[-2.9, 2.75, 0.6]} intensity={7} />
      {/* Cool evening light through the shop window */}
      <spotLight
        position={[-0.6, 2.4, 4.5]}
        angle={0.9}
        penumbra={0.8}
        intensity={10}
        color="#8fa6c8"
        distance={9}
      />
    </group>
  );
}
