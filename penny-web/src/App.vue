<script setup lang="ts">
import { ref } from "vue";
import { es } from "./locales/es";

type CurrentUser = {
  id: string;
  email: string;
};

const text = es.login;
const email = ref("");
const password = ref("");
const showPassword = ref(false);
const submitting = ref(false);
const error = ref("");
const currentUser = ref<CurrentUser | null>(null);

async function signIn() {
  if (submitting.value) return;

  submitting.value = true;
  error.value = "";

  try {
    const response = await fetch("/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        email: email.value.trim(),
        password: password.value,
      }),
    });

    if (response.status === 401) {
      error.value = text.invalidCredentials;
      return;
    }

    if (!response.ok) {
      error.value = text.unavailable;
      return;
    }

    password.value = "";
    showPassword.value = false;
    await loadCurrentUser();
  } catch {
    error.value = text.unavailable;
  } finally {
    submitting.value = false;
  }
}

async function loadCurrentUser() {
  try {
    const response = await fetch("/auth/me", { cache: "no-store" });

    if (!response.ok) {
      error.value = text.sessionUnavailable;
      return;
    }

    currentUser.value = await response.json();
  } catch {
    error.value = text.sessionUnavailable;
  }
}
</script>

<template>
  <main class="auth-page">
    <section class="auth-card" aria-labelledby="auth-title">
      <div class="auth-brand">
        <img src="/penny-icon.png" alt="" width="48" height="48" />
        <span>Penny</span>
      </div>

      <template v-if="currentUser">
        <h1 id="auth-title">{{ text.sessionTitle }}</h1>
        <p class="session-email">{{ currentUser.email }}</p>
      </template>

      <template v-else>
        <h1 id="auth-title">{{ text.title }}</h1>

        <form class="auth-form" @submit.prevent="signIn">
          <div class="auth-field">
            <label for="email">{{ text.emailLabel }}</label>
            <input
              id="email"
              v-model="email"
              type="email"
              autocomplete="username"
              required
            />
          </div>

          <div class="auth-field">
            <label for="password">{{ text.passwordLabel }}</label>
            <div class="password-field">
              <input
                id="password"
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="current-password"
                required
              />
              <button class="password-toggle" type="button" aria-controls="password" @click="showPassword = !showPassword">
                {{ showPassword ? text.hidePassword : text.showPassword }}
              </button>
            </div>
          </div>

          <p v-if="error" class="auth-error" role="alert">{{ error }}</p>

          <button class="auth-submit" type="submit" :disabled="submitting">
            {{ submitting ? text.submitting : text.submit }}
          </button>
        </form>
      </template>
    </section>
  </main>
</template>
