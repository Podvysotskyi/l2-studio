<script setup lang="ts">
defineProps<{
  title: string
  description: string
  icon: string
  eyebrow?: string
}>()
</script>

<template>
  <header class="flex flex-wrap items-start justify-between gap-6">
    <div class="flex min-w-0 items-start gap-4">
      <div
        class="grid size-11 shrink-0 place-items-center rounded-xl bg-primary/10 ring-1 ring-primary/20"
      >
        <UIcon :name="icon" class="size-5 text-primary" />
      </div>
      <div class="min-w-0">
        <p
          v-if="eyebrow"
          class="mb-1 text-xs font-semibold tracking-widest text-primary uppercase"
        >
          {{ eyebrow }}
        </p>
        <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
          {{ title }}
        </h1>
        <p class="mt-1 max-w-3xl text-sm leading-6 text-muted">
          {{ description }}
        </p>
      </div>
    </div>
    <div v-if="$slots.actions" class="flex items-center gap-2">
      <slot name="actions" />
    </div>
  </header>
</template>
