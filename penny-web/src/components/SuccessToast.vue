<script setup lang="ts">
import { nextTick, onUnmounted, ref } from "vue";

const duration = 4000;
const popover = ref<HTMLElement>();
const message = ref("");
const revision = ref(0);
let timeout: ReturnType<typeof setTimeout> | undefined;

function clear() {
  clearTimeout(timeout);
  popover.value?.hidePopover?.();
  message.value = "";
}

async function show(text: string) {
  clearTimeout(timeout);
  const currentRevision = ++revision.value;
  message.value = text;
  await nextTick();
  if (revision.value !== currentRevision || !popover.value) return;

  popover.value.showPopover?.();
  timeout = setTimeout(clear, duration);
}

onUnmounted(() => clearTimeout(timeout));
defineExpose({ show });
</script>

<template>
  <div class="announcement" role="status" aria-atomic="true">
    <span v-if="message" :key="revision">{{ message }}</span>
  </div>
  <div
    v-if="message"
    :key="revision"
    ref="popover"
    class="toast"
    popover="manual"
    aria-hidden="true"
  >
    <span class="toast-icon">✓</span>
    {{ message }}
  </div>
</template>

<style scoped>
.announcement {
  position: absolute;
  inline-size: 1px;
  block-size: 1px;
  overflow: hidden;
  clip-path: inset(50%);
  white-space: nowrap;
}

.toast {
  position: fixed;
  z-index: 10;
  inset: auto max(1rem, env(safe-area-inset-right)) max(1rem, env(safe-area-inset-bottom)) auto;
  margin: 0;
  inline-size: max-content;
  max-inline-size: min(25rem, calc(100% - 2rem));
  padding: 0.75rem 1rem;
  border: 1px solid #a7f3d0;
  border-radius: var(--radius-control);
  background: #d1fae5;
  color: var(--color-primary);
  box-shadow: 0 8px 24px rgb(15 23 42 / 15%);
  font-weight: 600;
  overflow-wrap: anywhere;
  pointer-events: none;
}

.toast-icon {
  margin-inline-end: 0.375rem;
  color: var(--color-income);
}

@media (prefers-reduced-motion: no-preference) {
  .toast {
    transition: opacity 180ms ease-out, transform 180ms ease-out, overlay 180ms;
    transition-behavior: allow-discrete;
  }

  @starting-style {
    .toast {
      opacity: 0;
      transform: translateY(0.75rem);
    }
  }
}
</style>
