<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import { deleteItemDefinition, getItemLookups, updateItemDefinition } from '../../../services/studio-api'
import type { ItemLookupKind, ItemLookupRecord, ItemRecord } from '../../../types/models/item'
import type { ItemFamily } from '../../../types/requests/directory-request'
import { loadDirectoryOptions } from '../../../utils/directory-pages'

const props = defineProps<{
  items: ItemRecord[]
  total: number
  loading: boolean
  error?: string
  family: ItemFamily
}>()
const query = defineModel<string>('query', { required: true })
const page = defineModel<number>('page', { required: true })
const pageSize = defineModel<number>('pageSize', { required: true })
const itemTypeName = defineModel<string | undefined>('itemTypeName', { required: true })
const itemActionName = defineModel<string | undefined>('itemActionName', { required: true })
const itemBodyPartName = defineModel<string | undefined>('itemBodyPartName', { required: true })
const itemMaterialName = defineModel<string | undefined>('itemMaterialName', { required: true })
const itemCrystalTypeName = defineModel<string | undefined>('itemCrystalTypeName', { required: true })
const handlerName = defineModel<string | undefined>('handlerName', { required: true })
const emit = defineEmits<{ refresh: [] }>()
const dialogs = useStudioDialogs()
const notifications = useStudioToasts()
const deletingId = ref<number>()
const selectedItem = ref<ItemRecord>()
const editOpen = ref(false)
const saving = ref(false)
const {
  pageError: editError,
  capture: captureEditError,
  clear: clearEditError,
  fieldError: editFieldError,
  set: setEditError
} = useStudioApiError()
const filtersLoading = ref(true)
const lookupOptions = ref<Partial<Record<ItemLookupKind, ItemLookupRecord[]>>>({})
const editForm = reactive({
  name: '',
  itemTypeName: '',
  itemActionName: null as string | null,
  itemBodyPartName: null as string | null,
  itemMaterialName: null as string | null,
  itemCrystalTypeName: null as string | null,
  handlerName: null as string | null,
  icon: '',
  weight: null as number | null,
  price: null as number | null,
  hasAttackGeometry: false,
  attackGeometryOffsetX: 0,
  attackGeometryOffsetY: 0,
  attackGeometryRadius: 0,
  attackGeometryLength: 0
})
const hasBodyAndCrystal = computed(() => ['armor', 'weapon', 'arrow', 'etc'].includes(props.family))
const hasHandler = computed(() => ['potion', 'recipe', 'enchant', 'scroll', 'pet-collar', 'etc'].includes(props.family))
const columns = computed<TableColumn<ItemRecord>[]>(() => [
  { accessorKey: 'id', header: 'ID' },
  { accessorKey: 'name', header: 'Name' },
  { accessorKey: 'itemTypeDisplayName', header: 'Type' },
  ...(hasHandler.value ? [{ accessorKey: 'handlerDisplayName', header: 'Handler' }] : []),
  ...(hasBodyAndCrystal.value ? [{ accessorKey: 'itemBodyPartDisplayName', header: 'Body part' }] : []),
  { accessorKey: 'itemMaterialDisplayName', header: 'Material' },
  ...(hasBodyAndCrystal.value ? [{ accessorKey: 'itemCrystalTypeDisplayName', header: 'Crystal type' }] : []),
  { accessorKey: 'price', header: 'Price' },
  { accessorKey: 'weight', header: 'Weight' },
  { id: 'actions', header: '' }
])
const filterValues = computed({
  get: () => ({
    itemTypeName: itemTypeName.value,
    itemActionName: itemActionName.value,
    itemBodyPartName: itemBodyPartName.value,
    itemMaterialName: itemMaterialName.value,
    itemCrystalTypeName: itemCrystalTypeName.value,
    handlerName: handlerName.value
  }),
  set: (value: Record<string, string | number | boolean | undefined>) => {
    itemTypeName.value = stringValue(value.itemTypeName)
    itemActionName.value = stringValue(value.itemActionName)
    itemBodyPartName.value = stringValue(value.itemBodyPartName)
    itemMaterialName.value = stringValue(value.itemMaterialName)
    itemCrystalTypeName.value = stringValue(value.itemCrystalTypeName)
    handlerName.value = stringValue(value.handlerName)
  }
})
const showItemTypeFilter = computed(() => filteredLookupOptions('item-types').length > 1)
const filters = computed(() => [
  ...(showItemTypeFilter.value ? [lookupFilter('itemTypeName', 'All types', 'item-types')] : []),
  ...(props.family !== 'material' ? [lookupFilter('itemActionName', 'All actions', 'item-actions')] : []),
  ...(hasBodyAndCrystal.value ? [lookupFilter('itemBodyPartName', 'All body parts', 'item-body-parts')] : []),
  lookupFilter('itemMaterialName', 'All materials', 'item-materials'),
  ...(hasBodyAndCrystal.value ? [lookupFilter('itemCrystalTypeName', 'All crystal types', 'item-crystal-types')] : []),
  ...(hasHandler.value ? [lookupFilter('handlerName', 'All handlers', 'item-handlers')] : [])
])
const itemActionOptions = computed(() => optionalLookupSelectOptions('item-actions'))
const itemBodyPartOptions = computed(() => optionalLookupSelectOptions('item-body-parts'))
const itemMaterialOptions = computed(() => optionalLookupSelectOptions('item-materials'))
const itemCrystalTypeOptions = computed(() => optionalLookupSelectOptions('item-crystal-types'))
const handlerOptions = computed(() => optionalLookupSelectOptions('item-handlers'))
const definitionTitle = computed(() => ({
  armor: 'Armor definitions',
  weapon: 'Weapon definitions', arrow: 'Arrow definitions', material: 'Material definitions',
  potion: 'Potion definitions', recipe: 'Recipe definitions', enchant: 'Enchant definitions',
  scroll: 'Scroll definitions', 'pet-collar': 'Pet Collar definitions', etc: 'Etc Item definitions'
}[props.family]))
const definitionDescription = computed(() => ({
  armor: 'C1 Mobius armor catalogue and combat statistics.',
  weapon: 'C1 Mobius weapon catalogue and combat statistics.', arrow: 'C1 Mobius ammunition catalogue.',
  material: 'C1 Mobius crafting material catalogue.', potion: 'C1 Mobius potion catalogue.',
  recipe: 'C1 Mobius recipe catalogue.', enchant: 'C1 Mobius enchant scroll catalogue.',
  scroll: 'C1 Mobius scroll catalogue.', 'pet-collar': 'C1 Mobius pet collar catalogue.',
  etc: 'C1 Mobius remaining item catalogue.'
}[props.family]))

async function loadFilters() {
  filtersLoading.value = true
  try {
    const kinds: ItemLookupKind[] = ['item-types', 'item-actions', 'item-body-parts', 'item-materials', 'item-crystal-types', 'item-handlers']
    const values = await Promise.all(kinds.map(async kind => [
      kind,
      await loadDirectoryOptions((nextPage, nextPageSize) => getItemLookups(kind, { page: nextPage, pageSize: nextPageSize }))
    ] as const))
    lookupOptions.value = Object.fromEntries(values)
    clearEditError()
  } catch (cause) {
    captureEditError(cause, 'The item lookup values could not be loaded.')
  } finally {
    filtersLoading.value = false
  }
}

async function refreshDirectory() {
  emit('refresh')
  await loadFilters()
}

async function remove(item: ItemRecord) {
  const confirmed = await dialogs.confirm({
    title: `Delete ${item.name}?`,
    description: `Item #${item.id} and its statistics will be permanently removed. A later import can restore the source record.`,
    confirmLabel: 'Delete item',
    confirmColor: 'error'
  })
  if (!confirmed) return
  deletingId.value = item.id
  try {
    await deleteItemDefinition(props.family, item.id)
    notifications.success({ title: 'Item definition deleted' })
    emit('refresh')
  } catch {
    notifications.error({ title: 'Item definition could not be deleted' })
  } finally {
    deletingId.value = undefined
  }
}

async function edit(item: ItemRecord) {
  selectedItem.value = item
  Object.assign(editForm, {
    name: item.name,
    itemTypeName: item.itemTypeName,
    itemActionName: item.itemActionName,
    itemBodyPartName: item.itemBodyPartName,
    itemMaterialName: item.itemMaterialName,
    itemCrystalTypeName: item.itemCrystalTypeName,
    handlerName: item.handlerName,
    icon: item.icon ?? '',
    weight: item.weight,
    price: item.price,
    hasAttackGeometry: item.attackGeometry !== null,
    attackGeometryOffsetX: item.attackGeometry?.offsetX ?? 0,
    attackGeometryOffsetY: item.attackGeometry?.offsetY ?? 0,
    attackGeometryRadius: item.attackGeometry?.radius ?? 0,
    attackGeometryLength: item.attackGeometry?.length ?? 0
  })
  clearEditError()
  editOpen.value = true
  if (!Object.keys(lookupOptions.value).length) await loadFilters()
}

async function save() {
  const item = selectedItem.value
  const name = editForm.name.trim()
  if (!item) return
  if (!name || name.length > 100) {
    setEditError('Name must contain between 1 and 100 characters.', {
      definition: ['Name must contain between 1 and 100 characters.']
    })
    return
  }
  saving.value = true
  clearEditError()
  try {
    const { itemTypeName: _, hasAttackGeometry, attackGeometryOffsetX, attackGeometryOffsetY, attackGeometryRadius, attackGeometryLength, ...definition } = editForm
    await updateItemDefinition(props.family, item.id, {
      ...definition,
      name,
      attackGeometry: hasAttackGeometry
        ? { offsetX: attackGeometryOffsetX, offsetY: attackGeometryOffsetY, radius: attackGeometryRadius, length: attackGeometryLength }
        : null
    })
    editOpen.value = false
    notifications.success({ title: 'Item definition saved' })
    emit('refresh')
  } catch (cause) {
    captureEditError(cause, 'The item definition could not be saved. Check the selected lookup values and try again.')
  } finally {
    saving.value = false
  }
}

function lookupFilter(key: string, placeholder: string, kind: ItemLookupKind) {
  return {
    key,
    placeholder,
    ariaLabel: placeholder,
    loading: filtersLoading.value,
    items: filteredLookupOptions(kind).map(item => ({
      label: kind === 'item-types' ? lookupValueLabel(item) : lookupLabel(item),
      value: item.name
    }))
  }
}

function lookupSelectOptions(kind: ItemLookupKind) {
  return filteredLookupOptions(kind).map(item => ({
    label: lookupLabel(item),
    value: item.name
  }))
}

function optionalLookupSelectOptions(kind: ItemLookupKind) {
  return [{ label: 'Unassigned', value: null }, ...lookupSelectOptions(kind)]
}

function filteredLookupOptions(kind: ItemLookupKind) {
  const options = lookupOptions.value[kind] ?? []
  if (kind !== 'item-types') return options
  return options.filter(item => matchesItemFamily(item))
}

function matchesItemFamily(item: ItemLookupRecord) {
  const root = item.parentTypeName ?? item.name
  if (props.family === 'weapon' || props.family === 'armor') return root === (props.family === 'weapon' ? 'Weapon' : 'Armor')
  if (props.family === 'enchant') return item.name === 'SCRL_ENCHANT_AM' || item.name === 'SCRL_ENCHANT_WP'
  if (props.family === 'pet-collar') return item.name === 'PET_COLLAR'
  if (props.family === 'etc') return root === 'EtcItem' && !['ARROW', 'MATERIAL', 'POTION', 'RECIPE', 'SCRL_ENCHANT_AM', 'SCRL_ENCHANT_WP', 'SCROLL', 'PET_COLLAR'].includes(item.name)
  return item.name === props.family.toUpperCase()
}

function lookupLabel(item: ItemLookupRecord) {
  const label = lookupValueLabel(item)
  if (!item.parentTypeName) return label
  const parent = item.parentTypeDisplayName === item.parentTypeName
    ? item.parentTypeName
    : `${item.parentTypeDisplayName} (${item.parentTypeName})`
  return `${parent} › ${label}`
}

function lookupValueLabel(item: ItemLookupRecord) {
  return item.displayName === item.name ? item.name : `${item.displayName} (${item.name})`
}

function itemTypeLabel(item: ItemRecord) {
  if (!item.itemParentTypeName) return item.itemTypeDisplayName
  return `${item.itemParentTypeDisplayName ?? item.itemParentTypeName} › ${item.itemTypeDisplayName}`
}

function viewTo(item: ItemRecord) {
  return `/authoring/items/${props.family}/${item.id}`
}

function stringValue(value: string | number | boolean | undefined) {
  return typeof value === 'string' ? value : undefined
}

watch([showItemTypeFilter, filtersLoading], ([show, loading]) => {
  if (!loading && !show) itemTypeName.value = undefined
})

onMounted(() => void loadFilters())
</script>

<template>
  <StudioContentDirectoryLayout
    :title="definitionTitle"
    :description="definitionDescription"
    icon="i-lucide-package-search"
    import-target="items"
    import-label="items"
    :loading="loading"
    :error="error"
    @refresh="refreshDirectory"
  >
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <StudioDataTable
        v-model:query="query"
        v-model:filter-values="filterValues"
        v-model:page="page"
        v-model:page-size="pageSize"
        :data="items"
        :total="total"
        :columns="columns"
        :filters="filters"
        :loading="loading"
        :empty="`No ${definitionTitle.toLowerCase()} match these filters.`"
        search-placeholder="Search item names"
        search-aria-label="Search item names"
        :page-size-options="[10, 25, 50, 100]"
        table-class="min-w-[78rem]"
      >
        <template #toolbar-start>
          <div>
            <p class="text-sm font-medium text-highlighted">Item catalog</p>
            <p class="text-xs text-muted">{{ total.toLocaleString() }} definitions</p>
          </div>
        </template>
        <template #actions-cell="{ row }">
          <StudioTableRowActions
            :view-to="viewTo(row.original)"
            :show-edit="true"
            :show-delete="true"
            :delete-loading="deletingId === row.original.id"
            @edit="edit(row.original)"
            @delete="remove(row.original)"
          />
        </template>
        <template #itemBodyPartDisplayName-cell="{ row }">
          {{ row.original.itemBodyPartDisplayName ?? '—' }}
        </template>
        <template #itemTypeDisplayName-cell="{ row }">
          {{ itemTypeLabel(row.original) }}
        </template>
        <template #handlerDisplayName-cell="{ row }">
          {{ row.original.handlerDisplayName ?? '—' }}
        </template>
        <template #itemMaterialDisplayName-cell="{ row }">
          {{ row.original.itemMaterialDisplayName ?? '—' }}
        </template>
        <template #itemCrystalTypeDisplayName-cell="{ row }">
          {{ row.original.itemCrystalTypeDisplayName ?? '—' }}
        </template>
      </StudioDataTable>
    </UCard>

    <UModal v-model:open="editOpen" title="Edit item definition">
      <template #body>
        <form class="space-y-4" @submit.prevent="save">
          <UAlert v-if="editError" color="error" variant="subtle" :description="editError" />
          <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
            <UFormField label="Name" required :error="editFieldError('definition')"><UInput v-model="editForm.name" maxlength="100" class="w-full" /></UFormField>
            <UFormField label="Type"><UInput :model-value="selectedItem ? itemTypeLabel(selectedItem) : ''" disabled class="w-full" /></UFormField>
            <UFormField v-if="props.family !== 'material'" label="Action"><USelect v-model="editForm.itemActionName" :items="itemActionOptions" :loading="filtersLoading" class="w-full" /></UFormField>
            <UFormField v-if="['armor', 'weapon', 'arrow', 'etc'].includes(props.family)" label="Body part"><USelect v-model="editForm.itemBodyPartName" :items="itemBodyPartOptions" :loading="filtersLoading" class="w-full" /></UFormField>
            <UFormField label="Material"><USelect v-model="editForm.itemMaterialName" :items="itemMaterialOptions" :loading="filtersLoading" class="w-full" /></UFormField>
            <UFormField v-if="['armor', 'weapon', 'arrow', 'etc'].includes(props.family)" label="Crystal type"><USelect v-model="editForm.itemCrystalTypeName" :items="itemCrystalTypeOptions" :loading="filtersLoading" class="w-full" /></UFormField>
            <UFormField v-if="['potion', 'recipe', 'enchant', 'scroll', 'pet-collar', 'etc'].includes(props.family)" label="Handler"><USelect v-model="editForm.handlerName" :items="handlerOptions" :loading="filtersLoading" class="w-full" /></UFormField>
            <UFormField label="Icon"><UInput v-model="editForm.icon" class="w-full" /></UFormField>
            <UFormField label="Weight"><UInput v-model.number="editForm.weight" type="number" class="w-full" /></UFormField>
            <UFormField label="Price"><UInput v-model.number="editForm.price" type="number" class="w-full" /></UFormField>
            <div v-if="props.family === 'weapon'" class="col-span-full space-y-3 rounded-md border border-default p-3">
              <UCheckbox v-model="editForm.hasAttackGeometry" label="Client attack geometry" />
              <div v-if="editForm.hasAttackGeometry" class="grid grid-cols-1 gap-4 md:grid-cols-2">
                <UFormField label="Start offset X" :error="editFieldError('attackGeometry')"><UInput v-model.number="editForm.attackGeometryOffsetX" type="number" class="w-full" /></UFormField>
                <UFormField label="Start offset Y" :error="editFieldError('attackGeometry')"><UInput v-model.number="editForm.attackGeometryOffsetY" type="number" class="w-full" /></UFormField>
                <UFormField label="Sweep radius" :error="editFieldError('attackGeometry')"><UInput v-model.number="editForm.attackGeometryRadius" type="number" min="0" class="w-full" /></UFormField>
                <UFormField label="Forward length" :error="editFieldError('attackGeometry')"><UInput v-model.number="editForm.attackGeometryLength" type="number" min="0" class="w-full" /></UFormField>
              </div>
            </div>
          </div>
          <div class="flex justify-end gap-3 pt-2"><UButton label="Cancel" color="neutral" variant="outline" @click="editOpen = false" /><UButton type="submit" label="Save changes" icon="i-lucide-save" :loading="saving" :disabled="filtersLoading" /></div>
        </form>
      </template>
    </UModal>
  </StudioContentDirectoryLayout>
</template>
