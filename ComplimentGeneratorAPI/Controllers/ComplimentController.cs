using Microsoft.AspNetCore.Mvc;
using System;
namespace ComplimentGeneratorAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class ComplimentController : ControllerBase
  {
    private static readonly string[] Compliments = new[]
    {
        "You're a shining star!",
        "Your smile is contagious.",
        "You're an inspiration to others.",
        "You have a heart of gold.",
        "You light up the room.",
        "You're a superhero without a cape.",
        "You bring joy wherever you go."
    };

    [HttpGet]
    public ActionResult<string> GetRandomCompliment()
    {
      Random random = new Random();
      int index = random.Next(Compliments.Length);
      return Ok(Compliments[index]);
    }
  }
}


// **1. precious** – qimmatli, aziz => preSHəs
// **2. excavation** – qazish, qazilma ish
// **3. seaside** – dengiz bo‘yi, sohil
// **4. bury** – ko`mmoq, dafn qilmoq.
// **5. eruption** – otilish (vulqon), portlash









// **6. lax** – bo`sh, sust
// **7. flimsy** – qo`l uchida qilingan
// **8. partition** – bo‘linma, ajratma, devor (kompyuterda: disk bo‘linmasi)
// **9. get into ** – kirmoq
// **10. exhibit** – ko‘rgazmaga qo‘ymoq, namoyish qilmoq







// **11. publish** – nashr etmoq, chop etmoq
// **12. inlay** – ichiga o‘rnatmoq, bezak sifatida joylashtirmoq, sayqallamoq
// **13. robbery** – o‘g‘rilik, talonchilik
// **14. adapt** – moslashmoq, moslashtirmoq
// **15. severe** – og‘ir, qattiq, jiddiy, juda yomon, 






// **16. admit** – tan olmoq, qabul qilmoq, yo‘l qo‘ymoq
// **17. truly** – chinakam, rostdan, haqiqatan
// **18. vastly** – juda katta darajada, nihoyatda
// **19. infer** – xulosa chiqarmoq
// **20. armed** – qurollangan





// **21. desire found** – istak topildi → “xohish aniqlandi” yoki “orzu topildi”
// **22. goq** – bu so‘z ma’nosiz, lekin “diagnosis” kontekstida “nosozlikni aniqlamoq” bo‘lishi mumkin
// **23. storeroom** – omborxona
// **24. tummy soil** – qorin tuprog‘i → ma’nosiz, lekin ehtimol “ich buzilishi” yoki “oshqozon bilan bog‘liq narsa”
// **25. smooth** – silliq


// **26. whenever** – qachonki, har safar

