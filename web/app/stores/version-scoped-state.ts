import { useAssetImportsStore } from './asset-imports'
import { useDashboardStore } from './dashboard'
import { useItemDirectoryStore } from './item-directory'
import { useItemRecipeDirectoryStore } from './item-recipe-directory'
import { useItemRecipeTypeDirectoryStore } from './item-recipe-type-directory'
import { useItemSetDirectoryStore } from './item-set-directory'
import { useItemTypeDirectoryStore } from './item-type-directory'
import { useLookupDirectoryStore } from './lookup-directory'
import { useNpcDirectoryStore } from './npc-directory'
import { useNpcLookupDirectoryStore } from './npc-lookup-directory'
import { usePlayerClassDirectoryStore } from './player-class-directory'
import { useSkillDirectoryStore } from './skill-directory'

export function resetVersionScopedState() {
  useAssetImportsStore().reset()
  useDashboardStore().reset()
  useItemDirectoryStore().reset()
  useItemRecipeDirectoryStore().reset()
  useItemRecipeTypeDirectoryStore().reset()
  useItemSetDirectoryStore().reset()
  useItemTypeDirectoryStore().reset()
  useLookupDirectoryStore().reset()
  useNpcDirectoryStore().reset()
  useNpcLookupDirectoryStore().reset()
  usePlayerClassDirectoryStore().reset()
  useSkillDirectoryStore().reset()
}
