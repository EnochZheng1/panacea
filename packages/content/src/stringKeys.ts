/**
 * The string keys every content entity must provide. Shared by the linter (which checks they
 * exist) and the client (which looks them up), so the two can never drift apart.
 */
export const stringKeys = {
  ingredient: (id: string) => [`ingredient.${id}.name`, `ingredient.${id}.description`],
  process: (id: string) => [`process.${id}.name`, `process.${id}.description`],
  unlock: (id: string) => [`unlock.${id}.name`, `unlock.${id}.description`],
  event: (id: string) => [`event.${id}.title`, `event.${id}.body`],
  eventChoice: (eventId: string, choiceId: string) => [`event.${eventId}.choice.${choiceId}`],
  timeline: (id: string) => [`timeline.${id}.headline`, `timeline.${id}.body`],
  background: (id: string) => [`background.${id}.name`, `background.${id}.description`],
  segment: (id: string) => [`segment.${id}.name`],
  ailment: (id: string) => [`ailment.${id}.name`],
  claim: (id: string) => [`claim.${id}.name`],
  verdict: (id: string) => [`verdict.${id}`],
  cause: (id: string) => [`cause.${id}`],
} as const;
