<script setup lang="ts">
const props = defineProps<{
  url?: string
  alt: string
  variant?: 'table' | 'header'
}>()

const failed = ref(false)
const isHeader = computed(() => props.variant === 'header')

watch(() => props.url, () => {
  failed.value = false
})
</script>

<template>
  <div
    class="grid shrink-0 place-items-center"
    :class="isHeader ? 'size-11 rounded-xl bg-primary/10 ring-1 ring-primary/20' : 'size-8 rounded bg-elevated'"
  >
    <img
      v-if="props.url && !failed"
      :src="props.url"
      :alt="`${props.alt} icon`"
      :width="isHeader ? 44 : 32"
      :height="isHeader ? 44 : 32"
      class="object-contain [image-rendering:pixelated]"
      :class="isHeader ? 'size-11' : 'size-8'"
      @error="failed = true"
    >
    <UIcon
      v-else
      name="i-lucide-image-off"
      :class="isHeader ? 'size-5 text-primary' : 'size-4 text-muted'"
      role="img"
      :aria-label="`${props.alt} icon unavailable`"
    />
  </div>
</template>
