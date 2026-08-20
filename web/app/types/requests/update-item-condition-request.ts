export interface UpdateItemConditionRequest {
  messageId: number
  addName: boolean
  isPvpFlagged: boolean | null
  playerRaces: string[]
  playerCategoryTypes: string[]
}
