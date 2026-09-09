# tilt-ball — Claude çalışma kuralları

Unity 6000.3.12f1 · MCP for Unity (com.coplaydev.unity-mcp) kurulu ve bağlı.

## Sahne / prefab / asset değişiklikleri

**`.unity`, `.prefab`, `.asset` dosyalarını diskten (Edit / Write / `sed -i`) DÜZENLEME.**

Bu dosyalar Unity Editor'de açıkken diskten değiştirilirse Unity dışarıdan
değişiklik algılar ve her seferinde kullanıcıya yeniden yükleme/onay diyaloğu
gösterir. Ayrıca Editor'ün bellekteki hali ile disk hali ayrışır; yanlış tarafın
kaydedilmesi yapılan işi geri alır.

Bunun yerine UnityMCP araçlarını kullan — değişiklik Editor'ün içinde uygulanır,
diyalog çıkmaz:

| Amaç | Araç |
|---|---|
| GameObject oluştur/değiştir/sil | `manage_gameobject` |
| Component ekle/çıkar, alan değeri yaz | `manage_components` |
| Sahne aç / kaydet / hiyerarşi | `manage_scene` |
| Prefab stage aç/kaydet/kapat | `manage_prefabs` |
| ScriptableObject (`.asset`) | `manage_scriptable_object` |
| Materyal, texture, import ayarı | `manage_material`, `manage_texture`, `manage_asset` |

Diskten düzenlemek gerçekten kaçınılmazsa: önce sahneyi Unity'de kapat/boşalt,
düzenle, sonra `refresh_unity` çağır — ve kullanıcıya haber ver.

## Scriptler (`.cs`)

Diskten düzenlemek sorun değil; Unity yalnızca yeniden derler, onay istemez.
Düzenleme sonrası gerekirse `refresh_unity(compile="request")`.

## Toplu dosya silme / taşıma

`git rm`, `rm -rf`, klasör taşıma gibi işlemler Assets/ altında yüzlerce harici
değişiklik üretir ve Unity'yi uzun bir reimport'a sokar. Önce kullanıcıdan onay
al, mümkünse Unity kapalıyken yap.

## Genel

- Değişikliği doğrulamadan "oldu" deme; `read_console` ile hata kontrolü yap.
- Ekran görüntüsü/geçici çıktıları `Assets/` altına yazma — scratchpad kullan.
