const KEY = "agripet_session";

export function getStorefrontUser() {
  try {
    const raw = localStorage.getItem(KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function setStorefrontUser(user) {
  localStorage.setItem(KEY, JSON.stringify(user));
}

export function clearStorefrontUser() {
  localStorage.removeItem(KEY);
}