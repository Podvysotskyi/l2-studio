<script setup lang="ts">
import { getItemDefinition, updateItemDefinition } from '~/services/studio-api'
import type { ItemRecord } from '~/types/models/item'

const route = useRoute()
const item = ref<ItemRecord>()
const error = ref<string>()
const saving = ref(false)
const toast = useStudioToasts()
const form = reactive({ name: '', itemTypeName: '', itemActionName: '', itemBodyPartName: '', itemMaterialName: '', itemCrystalTypeName: '', icon: '', weight: undefined as number | undefined, price: undefined as number | undefined, weaponType: '', armorType: '', etcItemType: '', damageRange: '' })
function apply(item: ItemRecord) {
  Object.assign(form, { name: item.name, itemTypeName: item.itemTypeName, itemActionName: item.itemActionName ?? '', itemBodyPartName: item.itemBodyPartName ?? '', itemMaterialName: item.itemMaterialName ?? '', itemCrystalTypeName: item.itemCrystalTypeName ?? '', icon: item.icon ?? '', weight: item.weight ?? undefined, price: item.price ?? undefined, weaponType: item.weaponType ?? '', armorType: item.armorType ?? '', etcItemType: item.etcItemType ?? '', damageRange: item.damageRange ?? '' })
}
async function load() { try { const result = await getItemDefinition(Number(route.params.id)); item.value = result; apply(result); error.value = undefined } catch { error.value = 'The item definition could not be loaded.' } }
async function save() {
  if (!item.value) return
  saving.value = true
  try { const result = await updateItemDefinition(item.value.id, form); item.value = result; apply(result); toast.success({ title: 'Item definition saved' }) }
  catch { toast.error({ title: 'Item definition could not be saved' }) }
  finally { saving.value = false }
}
watch(() => route.params.id, () => void load(), { immediate: true })
</script>

<template>
  <div class="space-y-4">
    <UAlert v-if="error" color="error" :description="error" />
    <template v-else-if="item">
      <UPageHeader :title="item.name" :description="`Item #${item.id} · ${item.itemTypeDisplayName}`" />
      <UCard><form class="grid grid-cols-1 gap-3 md:grid-cols-2" @submit.prevent="save"><UFormField label="Name"><UInput v-model="form.name" /></UFormField><UFormField label="Type"><UInput v-model="form.itemTypeName" /></UFormField><UFormField label="Action"><UInput v-model="form.itemActionName" /></UFormField><UFormField label="Body part"><UInput v-model="form.itemBodyPartName" /></UFormField><UFormField label="Material"><UInput v-model="form.itemMaterialName" /></UFormField><UFormField label="Crystal type"><UInput v-model="form.itemCrystalTypeName" /></UFormField><UFormField label="Icon"><UInput v-model="form.icon" /></UFormField><UFormField label="Weight"><UInput v-model.number="form.weight" type="number" /></UFormField><UFormField label="Price"><UInput v-model.number="form.price" type="number" /></UFormField><UFormField label="Weapon type"><UInput v-model="form.weaponType" /></UFormField><UFormField label="Armor type"><UInput v-model="form.armorType" /></UFormField><UFormField label="Etc item type"><UInput v-model="form.etcItemType" /></UFormField><UFormField label="Damage range"><UInput v-model="form.damageRange" /></UFormField><div class="col-span-full"><UButton type="submit" :loading="saving">Save overview</UButton></div></form></UCard>
      <UCard v-if="item.stats"><template #header>Statistics</template><pre class="text-xs">{{ item.stats }}</pre></UCard>
    </template>
  </div>
</template>
