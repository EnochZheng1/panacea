export * from './core/index.ts';
export {
  applyAction,
  applyActionInPlace,
  canonicalAction,
  endTurn,
  endTurnInPlace,
  newGame,
  replay,
  PHASES,
  type NewGameOptions,
} from './game.ts';
export { checkInvariants } from './invariants.ts';
export { deserializeGame, serializeGame, type SaveFile } from './save.ts';
export {
  legalActions,
  looksFor,
  moodFor,
  observe,
  type ActionSpec,
  type Looks,
  type Mood,
  type Observation,
  type ObservedCustomer,
  type ObservedInvestment,
  type ObservedDeath,
} from './api/machine.ts';
export {
  analyzeFormula,
  claimAppeal,
  claimBoldness,
  validateClaims,
  validateFormula,
  type FormulaAnalysis,
} from './systems/formula.ts';
export { maxBrewable, stockOf, stockValue, totalStock, type StockValue } from './systems/lab.ts';
export { acuteDanger, feltEffect, overdoseChance, reliefScore } from './systems/pharmacology.ts';
export {
  investRefusal,
  isHeld,
  orderPriceFactor,
  wholesaleOrder,
  wholesalePrice,
} from './systems/investments.ts';
export { availableChoices, nextUnlocks, type UnlockProgress } from './systems/progression.ts';
export {
  bribeCostCents,
  bribeExposureChance,
  implicationChance,
  INQUEST_EVENT_ID,
} from './systems/scrutiny.ts';
export {
  evaluateRequirement,
  requirementProgress,
  type ConditionProgress,
  type RequirementStatus,
} from './systems/requirements.ts';
