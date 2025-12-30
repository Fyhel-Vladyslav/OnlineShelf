import axios from "axios";

export const httpClient = axios.create({
  baseURL: "https://localhost:44343",
  headers: {
    "Content-Type": "application/json",
  },
});