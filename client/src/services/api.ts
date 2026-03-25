// שירות API — axios עם JWT אוטומטי
import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ? `${import.meta.env.VITE_API_URL}/api` : '/api',
});

// הוספת טוקן אוטומטי לכל בקשה
api.interceptors.request.use((config) => {
  const stored = localStorage.getItem('smarthome-auth');
  if (stored) {
    const { state } = JSON.parse(stored);
    if (state?.token) {
      config.headers.Authorization = `Bearer ${state.token}`;
    }
  }
  return config;
});

export default api;
