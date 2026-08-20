namespace WeeksLeft;

/// <summary>
/// Life expectancy at birth (e0) by country and sex.
/// Values are approximate UN WPP 2024 / WHO GHO era estimates, rounded to 0.1 years.
/// Format per record: ISO2 | name_ru | name_en | e0_male | e0_female
/// </summary>
public sealed record Country(string Iso2, string NameRu, string NameEn, double E0Male, double E0Female)
{
    public double E0(Sex sex) => sex switch
    {
        Sex.Male => E0Male,
        Sex.Female => E0Female,
        _ => (E0Male + E0Female) / 2.0
    };
}

public enum Sex { Male, Female, Average }

public static class LifeData
{
    public static readonly Country World = new("ZZ", "Мир (среднее)", "World (average)", 70.6, 75.9);

    private static readonly Dictionary<string, Country> _byIso = new(StringComparer.OrdinalIgnoreCase);
    public static IReadOnlyList<Country> All { get; }

    public static Country Get(string? iso2)
    {
        if (!string.IsNullOrWhiteSpace(iso2) && _byIso.TryGetValue(iso2.Trim(), out var c)) return c;
        return World;
    }

    static LifeData()
    {
        var list = new List<Country>();
        foreach (var line in Raw.Split('\n'))
        {
            var s = line.Trim();
            if (s.Length == 0 || s[0] == '#') continue;
            var p = s.Split('|');
            if (p.Length != 5) continue;
            var c = new Country(p[0], p[1], p[2],
                double.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture),
                double.Parse(p[4], System.Globalization.CultureInfo.InvariantCulture));
            list.Add(c);
            _byIso[c.Iso2] = c;
        }
        _byIso[World.Iso2] = World;
        list.Sort((a, b) => string.Compare(a.NameEn, b.NameEn, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, World);
        All = list;
    }

    private const string Raw = """
AD|Андорра|Andorra|81.0|86.0
AE|ОАЭ|United Arab Emirates|78.0|80.5
AF|Афганистан|Afghanistan|64.0|68.0
AL|Албания|Albania|77.0|81.0
AM|Армения|Armenia|71.5|78.9
AO|Ангола|Angola|60.0|65.5
AR|Аргентина|Argentina|74.0|80.9
AT|Австрия|Austria|79.8|84.4
AU|Австралия|Australia|81.3|85.3
AZ|Азербайджан|Azerbaijan|70.0|76.5
BA|Босния и Герцеговина|Bosnia and Herzegovina|74.5|79.8
BD|Бангладеш|Bangladesh|72.0|76.0
BE|Бельгия|Belgium|80.3|84.5
BF|Буркина-Фасо|Burkina Faso|59.5|62.5
BG|Болгария|Bulgaria|71.5|78.6
BH|Бахрейн|Bahrain|78.0|80.5
BI|Бурунди|Burundi|60.5|64.5
BJ|Бенин|Benin|59.0|62.5
BN|Бруней|Brunei|74.0|78.0
BO|Боливия|Bolivia|66.5|72.4
BR|Бразилия|Brazil|72.4|79.4
BT|Бутан|Bhutan|70.5|74.0
BW|Ботсвана|Botswana|65.0|71.0
BY|Беларусь|Belarus|69.7|79.6
CA|Канада|Canada|80.5|84.4
CD|ДР Конго|DR Congo|59.0|62.5
CF|ЦАР|Central African Republic|53.0|57.5
CG|Конго|Congo|62.5|66.5
CH|Швейцария|Switzerland|82.2|85.8
CI|Кот-д’Ивуар|Cote d'Ivoire|60.0|63.5
CL|Чили|Chile|78.5|83.7
CM|Камерун|Cameroon|60.0|63.5
CN|Китай|China|75.0|80.5
CO|Колумбия|Colombia|74.0|80.5
CR|Коста-Рика|Costa Rica|78.0|83.0
CU|Куба|Cuba|75.0|80.5
CY|Кипр|Cyprus|81.0|84.9
CZ|Чехия|Czechia|76.6|82.3
DE|Германия|Germany|78.9|83.6
DK|Дания|Denmark|79.9|83.7
DO|Доминиканская Республика|Dominican Republic|71.0|77.5
DZ|Алжир|Algeria|76.0|78.5
EC|Эквадор|Ecuador|74.5|79.9
EE|Эстония|Estonia|74.4|82.8
EG|Египет|Egypt|69.5|73.5
ER|Эритрея|Eritrea|64.0|69.0
ES|Испания|Spain|81.1|86.3
ET|Эфиопия|Ethiopia|63.0|68.0
FI|Финляндия|Finland|79.5|84.5
FJ|Фиджи|Fiji|65.0|71.0
FR|Франция|France|80.0|85.9
GA|Габон|Gabon|65.0|69.5
GB|Великобритания|United Kingdom|79.0|82.9
GE|Грузия|Georgia|68.5|77.5
GH|Гана|Ghana|62.5|65.5
GM|Гамбия|Gambia|62.0|66.0
GN|Гвинея|Guinea|58.5|61.0
GQ|Экваториальная Гвинея|Equatorial Guinea|60.0|63.5
GR|Греция|Greece|79.8|84.7
GT|Гватемала|Guatemala|68.5|74.7
HK|Гонконг|Hong Kong|82.5|88.0
HN|Гондурас|Honduras|70.5|76.3
HR|Хорватия|Croatia|75.6|81.7
HT|Гаити|Haiti|61.0|66.2
HU|Венгрия|Hungary|73.1|79.8
ID|Индонезия|Indonesia|66.5|71.0
IE|Ирландия|Ireland|81.0|84.4
IL|Израиль|Israel|81.0|84.8
IN|Индия|India|69.5|72.5
IQ|Ирак|Iraq|69.5|74.0
IR|Иран|Iran|74.5|78.5
IS|Исландия|Iceland|81.6|84.4
IT|Италия|Italy|81.4|85.6
JM|Ямайка|Jamaica|68.5|74.5
JO|Иордания|Jordan|75.0|79.0
JP|Япония|Japan|81.6|87.7
KE|Кения|Kenya|61.0|66.0
KG|Киргизия|Kyrgyzstan|68.0|75.5
KH|Камбоджа|Cambodia|67.0|72.5
KP|КНДР|North Korea|69.0|76.0
KR|Южная Корея|South Korea|80.6|86.5
KW|Кувейт|Kuwait|79.5|82.0
KZ|Казахстан|Kazakhstan|69.5|77.5
LA|Лаос|Laos|67.0|71.0
LB|Ливан|Lebanon|75.0|79.5
LK|Шри-Ланка|Sri Lanka|73.5|80.0
LR|Либерия|Liberia|60.0|63.0
LT|Литва|Lithuania|71.6|81.2
LU|Люксембург|Luxembourg|81.0|85.0
LV|Латвия|Latvia|70.6|80.1
LY|Ливия|Libya|69.5|74.5
MA|Марокко|Morocco|73.0|76.0
MD|Молдова|Moldova|68.5|76.9
ME|Черногория|Montenegro|74.5|79.3
MG|Мадагаскар|Madagascar|63.0|67.0
MK|Северная Македония|North Macedonia|74.0|78.3
ML|Мали|Mali|59.5|61.5
MM|Мьянма|Myanmar|63.5|70.5
MN|Монголия|Mongolia|66.0|74.5
MR|Мавритания|Mauritania|65.0|69.0
MT|Мальта|Malta|81.0|84.6
MU|Маврикий|Mauritius|71.0|78.0
MV|Мальдивы|Maldives|78.5|82.0
MW|Малави|Malawi|60.0|66.0
MX|Мексика|Mexico|71.5|77.9
MY|Малайзия|Malaysia|73.0|78.0
MZ|Мозамбик|Mozambique|57.0|63.5
NA|Намибия|Namibia|61.0|68.0
NE|Нигер|Niger|60.0|62.5
NG|Нигерия|Nigeria|52.5|54.5
NI|Никарагуа|Nicaragua|71.0|77.5
NL|Нидерланды|Netherlands|80.6|83.7
NO|Норвегия|Norway|81.6|84.8
NP|Непал|Nepal|68.5|72.5
NZ|Новая Зеландия|New Zealand|80.5|83.8
OM|Оман|Oman|76.5|80.5
PA|Панама|Panama|76.0|82.0
PE|Перу|Peru|74.0|79.5
PG|Папуа — Новая Гвинея|Papua New Guinea|63.0|67.5
PH|Филиппины|Philippines|67.5|74.5
PK|Пакистан|Pakistan|65.5|68.0
PL|Польша|Poland|74.4|82.0
PR|Пуэрто-Рико|Puerto Rico|76.5|84.0
PS|Палестина|Palestine|72.0|75.5
PT|Португалия|Portugal|78.6|84.4
PY|Парагвай|Paraguay|70.5|75.5
QA|Катар|Qatar|79.5|82.0
RO|Румыния|Romania|71.9|79.4
RS|Сербия|Serbia|73.5|78.9
RU|Россия|Russia|68.0|78.0
RW|Руанда|Rwanda|65.0|70.0
SA|Саудовская Аравия|Saudi Arabia|76.0|79.0
SD|Судан|Sudan|64.0|68.0
SE|Швеция|Sweden|81.5|85.0
SG|Сингапур|Singapore|81.5|86.1
SI|Словения|Slovenia|78.9|84.3
SK|Словакия|Slovakia|74.1|81.1
SL|Сьерра-Леоне|Sierra Leone|59.5|62.5
SN|Сенегал|Senegal|66.5|70.5
SO|Сомали|Somalia|54.0|58.5
SS|Южный Судан|South Sudan|55.0|58.5
SV|Сальвадор|El Salvador|68.5|77.5
SY|Сирия|Syria|70.0|76.0
SZ|Эсватини|Eswatini|57.0|65.0
TD|Чад|Chad|53.0|56.5
TG|Того|Togo|60.5|64.0
TH|Таиланд|Thailand|73.5|81.0
TJ|Таджикистан|Tajikistan|69.0|74.0
TL|Восточный Тимор|Timor-Leste|66.5|70.5
TM|Туркменистан|Turkmenistan|66.0|73.0
TN|Тунис|Tunisia|74.0|78.5
TR|Турция|Turkey|75.5|81.0
TW|Тайвань|Taiwan|78.0|84.5
TZ|Танзания|Tanzania|64.0|68.5
UA|Украина|Ukraine|66.0|76.5
UG|Уганда|Uganda|62.0|66.5
US|США|United States|75.8|81.1
UY|Уругвай|Uruguay|74.0|81.5
UZ|Узбекистан|Uzbekistan|69.5|74.5
VE|Венесуэла|Venezuela|68.0|76.5
VN|Вьетнам|Vietnam|71.0|79.5
XK|Косово|Kosovo|74.0|79.0
YE|Йемен|Yemen|65.0|69.5
ZA|ЮАР|South Africa|62.0|68.5
ZM|Замбия|Zambia|60.5|66.0
ZW|Зимбабве|Zimbabwe|58.5|64.5
""";
}
