import { nextTick, reactive } from "vue";
import { es } from "./locales/es";

type FormControl = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

export function useFieldValidation(formatMessages: Record<string, string> = {}) {
  const errors = reactive<Record<string, string | undefined>>({});

  function invalid(event: Event) {
    const field = event.target as FormControl;
    const key = field.id || field.name;
    if (!key) return;

    errors[key] = field.validity.valueMissing
      ? es.validation.required
      : formatMessages[key] ?? es.validation.invalid;

    if (field.form?.querySelector("input:invalid, select:invalid, textarea:invalid") === field) {
      void nextTick(() => field.focus());
    }
  }

  function clearValidError(event: Event) {
    const field = event.target as FormControl;
    if (field.validity?.valid) delete errors[field.id || field.name];
  }

  function attributes(key: string, hint?: string) {
    return {
      "aria-invalid": errors[key] ? true as const : undefined,
      "aria-describedby": [hint, errors[key] ? key + "-error" : undefined].filter(Boolean).join(" ") || undefined,
    };
  }

  function reset() {
    for (const key of Object.keys(errors)) delete errors[key];
  }

  return { errors, invalid, clearValidError, attributes, reset };
}
