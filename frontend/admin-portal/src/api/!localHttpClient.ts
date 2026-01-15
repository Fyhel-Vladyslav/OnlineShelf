import axios from "axios";

export const httpClient = axios.create({
  baseURL: "https://localhost:44300",
  headers: {
    "Content-Type": "application/json",
  },
});