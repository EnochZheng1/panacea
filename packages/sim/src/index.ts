export * from './core/index.ts';
export { applyAction, endTurn, newGame, replay, PHASES, type NewGameOptions } from './game.ts';
export { checkInvariants } from './invariants.ts';
export { deserializeGame, serializeGame, type SaveFile } from './save.ts';
export { legalActions, observe, type ActionSpec, type Observation } from './api/machine.ts';
export { analyzeFormula, validateFormula, type FormulaAnalysis } from './systems/formula.ts';
export { maxBrewable } from './systems/lab.ts';
export { nextUnlocks, type UnlockProgress } from './systems/progression.ts';
export { evaluateRequirement, type RequirementStatus } from './systems/requirements.ts';
