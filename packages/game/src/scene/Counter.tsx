import type { ObservedCustomer } from '@panacea/sim';
import { useFrame } from '@react-three/fiber';
import { useMemo, useRef, useState } from 'react';
import * as THREE from 'three';
import { Bottle } from './Bottle.tsx';
import { MAT } from './materials.ts';

export const COUNTER = { x: 0.35, z: 0.9, length: 3.2, height: 1.0, depth: 0.6 } as const;

export interface CounterProps {
  /** Bottles on display: one entry per formula with its colour and stock. */
  stock: { formulaId: string; color: string; count: number }[];
  customers: ObservedCustomer[];
  onCustomerHover?: (c: ObservedCustomer | null) => void;
}

/** The shop counter: mahogany, a glass display case of your bottles, a brass till. */
export function Counter({ stock, customers, onCustomerHover }: CounterProps) {
  const { x, z, length, height, depth } = COUNTER;
  const bottles = useMemo(() => {
    const out: { key: string; color: string; pos: [number, number, number] }[] = [];
    let i = 0;
    for (const s of stock) {
      for (let k = 0; k < Math.min(s.count, 24); k++, i++) {
        if (i >= 48) break;
        const col = i % 16;
        const row = Math.floor(i / 16);
        out.push({
          key: `${s.formulaId}-${k}`,
          color: s.color,
          pos: [x - length / 2 + 0.3 + col * 0.15, height + 0.01, z - 0.15 + row * 0.13],
        });
      }
    }
    return out;
  }, [stock, x, z, length, height]);

  return (
    <group>
      <mesh position={[x, height / 2, z]} material={MAT.darkWood} castShadow receiveShadow>
        <boxGeometry args={[length, height, depth]} />
      </mesh>
      <mesh position={[x, height + 0.01, z]} material={MAT.wood} castShadow receiveShadow>
        <boxGeometry args={[length + 0.08, 0.03, depth + 0.08]} />
      </mesh>
      {/* Front panels */}
      {Array.from({ length: 5 }, (_, i) => (
        <mesh
          key={i}
          position={[x - length / 2 + (length * (i + 0.5)) / 5, height / 2, z + depth / 2 + 0.01]}
          material={MAT.wood}
        >
          <boxGeometry args={[length / 5 - 0.08, height - 0.25, 0.02]} />
        </mesh>
      ))}
      {/* Glass display case on the counter */}
      <mesh
        ref={(m) => m && (m.userData['glass'] = true)}
        position={[x - 0.25, height + 0.19, z]}
        material={MAT.glass}
      >
        <boxGeometry args={[length - 0.8, 0.34, depth - 0.1]} />
      </mesh>
      {bottles.map((b) => (
        <Bottle key={b.key} shape="tall" color={b.color} fill={0.95} position={b.pos} scale={0.9} />
      ))}
      {/* Brass till */}
      <group position={[x + length / 2 - 0.3, height + 0.02, z - 0.05]}>
        <mesh material={MAT.brass} position={[0, 0.13, 0]} castShadow>
          <boxGeometry args={[0.36, 0.26, 0.32]} />
        </mesh>
        <mesh material={MAT.brass} position={[0, 0.32, -0.06]} rotation={[-0.4, 0, 0]} castShadow>
          <boxGeometry args={[0.3, 0.14, 0.04]} />
        </mesh>
      </group>
      <Customers customers={customers} {...(onCustomerHover ? { onHover: onCustomerHover } : {})} />
    </group>
  );
}

const HATS = ['top', 'bonnet', 'cap', 'bowler'] as const;

function hashOf(s: string) {
  let h = 0;
  for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0;
  return Math.abs(h);
}

/** Regulars waiting at the counter. Posture shows health; trembling shows withdrawal. */
function Customers({
  customers,
  onHover,
}: {
  customers: ObservedCustomer[];
  onHover?: (c: ObservedCustomer | null) => void;
}) {
  const shown = customers.slice(0, 6);
  return (
    <group>
      {shown.map((c, i) => (
        <Figure
          key={c.id}
          customer={c}
          position={[
            COUNTER.x - 1.1 + (i % 3) * 1.1 + (i >= 3 ? 0.5 : 0),
            0,
            COUNTER.z + 0.85 + (i >= 3 ? 0.75 : 0),
          ]}
          {...(onHover ? { onHover } : {})}
        />
      ))}
    </group>
  );
}

const FEMININE = ['mother', 'seamstress'];

/** A few engraved features: eyes, nose, and for some, whiskers or a bun. */
function Face({
  height,
  seed,
  skirt,
  ill,
}: {
  height: number;
  seed: number;
  skirt: boolean;
  ill: boolean;
}) {
  const y = height * 0.94;
  const whiskers = !skirt && seed % 3 !== 0;
  const beard = !skirt && seed % 5 === 0;
  return (
    <group position={[0, y, 0]}>
      {[-0.035, 0.035].map((x) => (
        <mesh key={x} position={[x, 0.02 - (ill ? 0.005 : 0), 0.088]} material={MAT.iron}>
          <sphereGeometry args={[ill ? 0.009 : 0.011, 8, 6]} />
        </mesh>
      ))}
      <mesh position={[0, -0.005, 0.098]} rotation={[0.3, 0, 0]} material={MAT.skin}>
        <coneGeometry args={[0.014, 0.04, 6]} />
      </mesh>
      {whiskers && (
        <mesh position={[0, -0.035, 0.088]} rotation={[0, 0, Math.PI / 2]} material={MAT.darkWood}>
          <capsuleGeometry args={[0.008, 0.06, 4, 8]} />
        </mesh>
      )}
      {beard && (
        <mesh position={[0, -0.07, 0.06]} material={MAT.darkWood}>
          <sphereGeometry args={[0.07, 12, 10, 0, Math.PI * 2, Math.PI / 2, Math.PI / 2]} />
        </mesh>
      )}
      {skirt && (
        <mesh position={[0, 0.05, -0.08]} material={MAT.darkWood}>
          <sphereGeometry args={[0.05, 10, 8]} />
        </mesh>
      )}
      {/* Hair */}
      <mesh position={[0, 0.03, -0.01]} material={MAT.darkWood}>
        <sphereGeometry args={[0.104, 16, 10, 0, Math.PI * 2, 0, Math.PI * 0.42]} />
      </mesh>
    </group>
  );
}

function Figure({
  customer,
  position,
  onHover,
}: {
  customer: ObservedCustomer;
  position: [number, number, number];
  onHover?: (c: ObservedCustomer | null) => void;
}) {
  const ref = useRef<THREE.Group>(null);
  const [hover, setHover] = useState(false);
  const h = hashOf(customer.id);
  const skirt = FEMININE.includes(customer.segment) || /^(Mrs|Miss|Widow)/.test(customer.name);
  const hat = skirt ? (h % 2 ? 'bonnet' : 'none') : HATS[h % HATS.length];
  const stoop = { hale: 0, peaky: 0.08, poorly: 0.2, gravely_ill: 0.35 }[customer.looks];
  const height = 1.5 + (h % 7) * 0.03 - (customer.segment === 'elderly' ? 0.06 : 0);

  const body = useMemo(() => {
    const profile = skirt
      ? [
          [0, 0],
          [0.32, 0],
          [0.24, 0.5],
          [0.16, 0.9],
          [0.17, 1.15],
          [0.12, 1.28],
          [0.05, 1.32],
          [0, 1.32],
        ]
      : [
          [0, 0],
          [0.09, 0],
          [0.11, 0.8],
          [0.2, 0.95],
          [0.2, 1.2],
          [0.13, 1.3],
          [0.05, 1.33],
          [0, 1.33],
        ];
    return new THREE.LatheGeometry(
      profile.map(([r, y]) => new THREE.Vector2(r!, (y! * height) / 1.5)),
      18,
    );
  }, [skirt, height]);

  useFrame(({ clock }) => {
    if (!ref.current) return;
    const t = clock.elapsedTime + (h % 100);
    ref.current.rotation.x = -stoop + Math.sin(t * 0.8) * 0.01;
    ref.current.position.x = position[0] + (customer.withdrawal ? Math.sin(t * 38) * 0.006 : 0);
  });

  return (
    <group
      ref={ref}
      position={position}
      rotation={[0, Math.PI, 0]}
      onPointerOver={(e) => {
        e.stopPropagation();
        setHover(true);
        onHover?.(customer);
      }}
      onPointerOut={() => {
        setHover(false);
        onHover?.(null);
      }}
    >
      <mesh
        geometry={body}
        material={skirt ? MAT.leather : MAT.cloth}
        castShadow
        scale={hover ? 1.03 : 1}
      />
      <mesh position={[0, height * 0.94, 0]} material={MAT.skin} castShadow>
        <sphereGeometry args={[0.1, 16, 12]} />
      </mesh>
      <Face height={height} seed={h} skirt={skirt} ill={stoop > 0.1} />
      {hat === 'top' && (
        <mesh position={[0, height * 0.94 + 0.13, 0]} material={MAT.iron} castShadow>
          <cylinderGeometry args={[0.085, 0.09, 0.2, 14]} />
        </mesh>
      )}
      {(hat === 'top' || hat === 'bowler') && (
        <mesh position={[0, height * 0.94 + 0.05, 0]} material={MAT.iron}>
          <cylinderGeometry args={[0.15, 0.15, 0.015, 16]} />
        </mesh>
      )}
      {hat === 'bowler' && (
        <mesh position={[0, height * 0.94 + 0.06, 0]} material={MAT.iron}>
          <sphereGeometry args={[0.1, 14, 10, 0, Math.PI * 2, 0, Math.PI / 2]} />
        </mesh>
      )}
      {hat === 'cap' && (
        <mesh
          position={[0, height * 0.94 + 0.07, 0.02]}
          material={MAT.cloth}
          rotation={[0.2, 0, 0]}
        >
          <cylinderGeometry args={[0.11, 0.12, 0.05, 14]} />
        </mesh>
      )}
      {hat === 'bonnet' && (
        <mesh position={[0, height * 0.94 + 0.02, -0.02]} material={MAT.paper}>
          <sphereGeometry args={[0.125, 14, 10, 0, Math.PI * 2, 0, Math.PI * 0.6]} />
        </mesh>
      )}
      {customer.looks === 'gravely_ill' && (
        <mesh position={[0.22, 0.45, 0.1]} rotation={[0.15, 0, 0.1]} material={MAT.darkWood}>
          <cylinderGeometry args={[0.012, 0.012, 0.9, 6]} />
        </mesh>
      )}
      {customer.consumer === 'infant' && (
        <mesh position={[0, height * 0.66, 0.16]} material={MAT.paper}>
          <sphereGeometry args={[0.09, 12, 10]} />
        </mesh>
      )}
    </group>
  );
}
