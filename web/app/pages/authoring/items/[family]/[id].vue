<script setup lang="ts">
import { isItemFamily } from '~/utils/item-family'

const route = useRoute()
const familyValue = Array.isArray(route.params.family) ? route.params.family[0] : route.params.family
if (!familyValue || !isItemFamily(familyValue)) throw createError({ statusCode: 404, statusMessage: 'Item family not found' })
</script>

<template><ItemDefinitionDetail :family="familyValue" /></template>
