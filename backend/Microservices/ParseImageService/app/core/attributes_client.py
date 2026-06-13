import os
import requests
import json
from collections import Counter

class AttributesServiceClient:
    def __init__(self):
        self.shelf_service_url = os.getenv("SHELF_SERVICE_URL", "http://localhost:5000") 
        self._cache = {}
        self.is_initialized = False

    def fetch_attributes(self) -> bool:
        url = f"{self.shelf_service_url}/shelfs/items/attributes"
        try:
            print(f"🔄 Запит на синхронізацію атрибутів з {url}...")
            response = requests.get(url, timeout=5)
            
            if response.status_code != 200:
                print(f"⚠️ ShelfService повернув статус {response.status_code}")
                return False

            raw_content = response.text
            try:
                parsed_json = json.loads(raw_content)
                if isinstance(parsed_json, str):
                    parsed_json = json.loads(parsed_json)
            except Exception as parse_err:
                print(f"❌ Помилка парсингу JSON: {parse_err}")
                return False

            # Дістаємо масив DTO з обгортки об'єкта (пробуємо різні регістри ключа)
            dto_list = []
            if isinstance(parsed_json, dict):
                dto_list = parsed_json.get("attributesValues") or parsed_json.get("AttributesValues") or []
            elif isinstance(parsed_json, list):
                dto_list = parsed_json

            new_cache = {}
            logging_stats = {} # Для збору статистики по attributeId / typeName

            # Проходимо по списку атрибутів
            for dto in dto_list:
                if not isinstance(dto, dict):
                    continue
                
                # Читаємо необхідні поля
                attr_id = dto.get("attributeId") or dto.get("AttributeId") or "UnknownID"
                type_name = dto.get("typeName") or dto.get("TypeName")
                options = dto.get("options") or dto.get("Options") or []
                
                if not type_name:
                    continue
                    
                options_dict = {}
                for opt in options:
                    if not isinstance(opt, dict):
                        continue
                        
                    key = opt.get("key") if opt.get("key") is not None else opt.get("Key")
                    value = opt.get("value") if opt.get("value") is not None else opt.get("Value")
                    
                    if key is not None and value is not None:
                        options_dict[int(key)] = str(value).lower().strip()
                
                # Зберігаємо в кеш під назвою типу для зручного пошуку ШІ
                new_cache[type_name] = options_dict
                
                # Зберігаємо статистику для красивого логу: "ID (Назва): Х опцій"
                logging_stats[f"ID {attr_id} ({type_name})"] = len(options_dict)
            
            self._cache = new_cache
            self.is_initialized = True
            
            # Гарне груповане виведення кількості атрибутів
            print("✅ Кеш атрибутів успішно оновлено!")
            print("📊 Статистика завантажених даних:")
            for attr_info, count in logging_stats.items():
                print(f"   🔹 {attr_info} ──► кількість значень: {count}")
                
            return True

        except Exception as e:
            print(f"❌ Критична помилка під час обробки відповіді: {e}")
            return False

    def get_key_by_value(self, attribute_name: str, value: str) -> int:
        category = self._cache.get(attribute_name, {})
        search_val = value.lower().strip()
        
        for key, val in category.items():
            if val == search_val:
                return key
        return 0

    def has_data(self) -> bool:
        return self.is_initialized and bool(self._cache)