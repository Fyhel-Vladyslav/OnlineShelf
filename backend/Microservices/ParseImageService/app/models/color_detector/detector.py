import cv2
import numpy as np
from sklearn.cluster import KMeans

class ColorDetector:
    def __init__(self):
        # Будемо шукати 3 основні кластери кольорів (наприклад: фон, колір речі, логотип/принт)
        self.n_clusters = 3

    def get_dominant_colors(self, crop_bgr):
        """
        Визначає головний та другорядний кольори на кропі зображення
        """
        # Зменшуємо розмір для прискорення обчислень
        img = cv2.resize(crop_bgr, (100, 100), interpolation=cv2.INTER_AREA)
        # Перетворюємо в одновимірний масив пікселів
        pixels = img.reshape(-1, 3)
        
        # Запускаємо K-Means кластеризацію
        kmeans = KMeans(n_clusters=self.n_clusters, n_init=10, random_state=42)
        labels = kmeans.fit_predict(pixels)
        
        # Рахуємо, скільки пікселів потрапило в кожен колір (щоб відсортувати за домінантністю)
        counts = np.bincount(labels)
        center_colors = kmeans.cluster_centers_
        
        # Сортуємо індекси за спаданням кількості пікселів
        ordered_indices = np.argsort(counts)[::-1]
        
        # Переводимо кольори з BGR (OpenCV) назад в RGB
        def bgr_to_name(bgr):
            r, g, b = int(bgr[2]), int(bgr[1]), int(bgr[0])
            # Для MVP повертаємо прості назви або HEX-коди. Поки повернемо текстові мітки.
            # Згодом тут можна написати мапінг на назви ("Чорний", "Червоний" тощо) через відстань кольорів
            if r < 50 and g < 50 and b < 50: return "Чорний"
            if r > 200 and g > 200 and b > 200: return "Білий"
            if r > g and r > b: return "Червоний"
            if b > r and b > g: return "Синій"
            if g > r and g > b: return "Зелений"
            return "Сірий"

        # Беремо перші два найбільш популярні кольори
        main_bgr = center_colors[ordered_indices[0]]
        second_bgr = center_colors[ordered_indices[1]] if len(ordered_indices) > 1 else main_bgr
        
        return {
            "main": bgr_to_name(main_bgr),
            "second": bgr_to_name(second_bgr)
        }