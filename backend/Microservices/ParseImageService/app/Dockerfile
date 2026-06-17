# Використовуємо офіційний легкий образ Python
FROM python:3.10-slim

# Встановлюємо системні залежності (необхідно для OpenCV / апдейтів)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgl1-mesa-glx \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

# Встановлюємо робочу директорію всередині контейнера
WORKDIR /app

# Спочатку копіюємо requirements і встановлюємо їх (це пришвидшить білд при зміні коду)
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Копіюємо решту вихідного коду проекту
COPY . .

# Вказуємо порт, який слухає твій Python-додаток (наприклад, FastAPI чи Flask)
EXPOSE 5000

# Команда для запуску (зміни під свій фреймворк, наприклад: uvicorn main:app --host 0.0.0.0 --port 5000)
CMD ["python", "main.py"]