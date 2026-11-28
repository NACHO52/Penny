<script lang="ts">
export type Profile = { id: string; userName: string; email: string };
export type ProfileUpdate = {
  userName: string;
  email: string;
  currentPassword: string;
  newPassword: string | null;
};
</script>

<script setup lang="ts">
import { ref, watch } from "vue";
import { es } from "../locales/es";

const props = defineProps<{
  profile: Profile;
  disabled: boolean;
  save: (request: ProfileUpdate) => Promise<Response | undefined>;
}>();
const text = es.profile;
const userName = ref(props.profile.userName);
const email = ref(props.profile.email);
const currentPassword = ref("");
const newPassword = ref("");
const saving = ref(false);
const errors = ref<string[]>([]);

watch(() => props.profile, (profile) => {
  userName.value = profile.userName;
  email.value = profile.email;
  currentPassword.value = "";
  newPassword.value = "";
});

async function saveProfile() {
  if (saving.value || props.disabled) return;
  const request = {
    userName: userName.value,
    email: email.value,
    currentPassword: currentPassword.value,
    newPassword: newPassword.value || null,
  };
  errors.value = [];
  saving.value = true;
  try {
    const response = await props.save(request);
    if (!response || response.status === 401) return;
    if (response.status === 429) {
      errors.value = [text.locked];
      return;
    }
    if (response.status === 400 || response.status === 409) {
      const { codes } = await response.json() as { codes?: string[] };
      errors.value = codes?.length
        ? codes.map(code => Object.prototype.hasOwnProperty.call(text.errors, code) ? text.errors[code] : text.saveError)
        : [text.saveError];
      return;
    }
    if (!response.ok) {
      errors.value = [text.saveError];
      return;
    }

  } catch {
    errors.value = [text.saveError];
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <form class="auth-form profile-form" @submit.prevent="saveProfile">
    <fieldset :disabled="disabled || saving">
      <legend>{{ text.details }}</legend>
      <div class="auth-field">
        <label for="profile-user-name">{{ text.userName }}</label>
        <input id="profile-user-name" v-model.trim="userName" name="userName" type="text" maxlength="256" autocapitalize="none" spellcheck="false" required />
      </div>
      <div class="auth-field">
        <label for="profile-email">{{ text.email }}</label>
        <p id="profile-email-hint" class="profile-field-hint">{{ text.emailHint }}</p>
        <input id="profile-email" v-model.trim="email" name="email" type="email" autocomplete="username" maxlength="256" aria-describedby="profile-email-hint" required />
      </div>
      <div class="auth-field">
        <label for="profile-current-password">{{ text.currentPassword }}</label>
        <p id="profile-current-password-hint" class="profile-field-hint">{{ text.currentPasswordHint }}</p>
        <input id="profile-current-password" v-model="currentPassword" name="currentPassword" type="password" autocomplete="current-password" aria-describedby="profile-current-password-hint" required />
      </div>
      <div class="auth-field">
        <label for="profile-new-password">{{ text.newPassword }}</label>
        <p id="profile-new-password-hint" class="profile-field-hint">{{ text.newPasswordHint }}</p>
        <input id="profile-new-password" v-model="newPassword" name="newPassword" type="password" autocomplete="new-password" minlength="6" aria-describedby="profile-new-password-hint" />
      </div>
      <button class="auth-submit" type="submit">{{ saving ? text.saving : text.save }}</button>
    </fieldset>
    <p v-if="errors.length" class="auth-error" role="alert">{{ errors.join(" ") }}</p>
  </form>
</template>
