import * as THREE from 'three';

/**
 * Shared materials. The ink pass turns everything into engraving, so these mostly control
 * *tone* (light and dark) and a whisper of hue; liquids use `liquidMaterial` and glow.
 */
export const MAT = {
  plaster: new THREE.MeshStandardMaterial({ color: '#cbb894', roughness: 0.95 }),
  wainscot: new THREE.MeshStandardMaterial({ color: '#5a3b24', roughness: 0.8 }),
  floor: new THREE.MeshStandardMaterial({ color: '#6e4a2c', roughness: 0.85 }),
  wood: new THREE.MeshStandardMaterial({ color: '#7a5233', roughness: 0.7 }),
  darkWood: new THREE.MeshStandardMaterial({ color: '#3c2617', roughness: 0.6 }),
  brass: new THREE.MeshStandardMaterial({ color: '#c79a43', roughness: 0.32, metalness: 0.85 }),
  copper: new THREE.MeshStandardMaterial({ color: '#b5653a', roughness: 0.35, metalness: 0.85 }),
  iron: new THREE.MeshStandardMaterial({ color: '#2a2724', roughness: 0.6, metalness: 0.6 }),
  brick: new THREE.MeshStandardMaterial({ color: '#7a3e2a', roughness: 0.95 }),
  paper: new THREE.MeshStandardMaterial({ color: '#f3e7cc', roughness: 1 }),
  leather: new THREE.MeshStandardMaterial({ color: '#4a2418', roughness: 0.75 }),
  cloth: new THREE.MeshStandardMaterial({ color: '#4c4a52', roughness: 0.95 }),
  skin: new THREE.MeshStandardMaterial({ color: '#d9b08c', roughness: 0.8 }),
  glass: new THREE.MeshStandardMaterial({
    color: '#d8e6e0',
    roughness: 0.08,
    metalness: 0,
    transparent: true,
    opacity: 0.28,
    depthWrite: false,
  }),
  windowGlass: new THREE.MeshStandardMaterial({
    color: '#9fb4c4',
    emissive: '#53677a',
    emissiveIntensity: 0.6,
    roughness: 0.1,
    transparent: true,
    opacity: 0.5,
  }),
};

const liquidCache = new Map<string, THREE.MeshBasicMaterial>();

/** Unlit, flat-coloured: the mask pass renders these as the glowing medicine. */
export function liquidMaterial(color: string): THREE.MeshBasicMaterial {
  let m = liquidCache.get(color);
  if (!m) {
    m = new THREE.MeshBasicMaterial({ color });
    liquidCache.set(color, m);
  }
  return m;
}

/** Marks a mesh for the ink pipeline. Use as a ref callback. */
export const asGlass = (m: THREE.Object3D | null) => {
  if (m) m.userData['glass'] = true;
};
export const asLiquid = (m: THREE.Object3D | null) => {
  if (m) m.userData['liquid'] = true;
};
