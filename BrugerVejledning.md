# Brugervejledning til Z21Dashboard

# Indholdsfortegnelse
*   [Indledning](#indledning)
    *   [Hvorfor Z21Dashboard?](#hvorfor-z21dashboard)
    *   [Lidt om teknikken](#lidt-om-teknikken)
*   [Z21 familien af centralstationer](#z21-familien-af-centralstationer)
    *   [Brug af protokol](#brug-af-protokol)
    *   [z21/z21Start og låst eller ej](#z21z21start-og-låst-eller-ej)
*   [Første gang du starter](#første-gang-du-starter)
*   [Dashboardet](#dashboardet)
    *   [Flyt rundt på vinduer](#flyt-rundt-på-vinduer)
    *   [Tilpas dit dashboard (vis/skjul vinduer)](#tilpas-dit-dashboard-visskjul-vinduer)
    *   [De vigtigste vinduer forklaret](#de-vigtigste-vinduer-forklaret)
*   [FAQ (OSS)](#faq-oss)
    *   [Hvad betyder "Låst" (Locked)?](#hvad-betyder-låst-locked)
    *   [Kan navne fra min multiMAUS eller Z21 app overføres til Z21Dashboard?](#kan-navne-fra-min-multimaus-eller-z21-app-overføres-til-z21dashboard)
    *   [Z21Dashboard kan ikke forbinde til min Z21](#z21dashboard-kan-ikke-forbinde-til-min-z21)
    *   [Vil du implementere widgets til LocoNet- og CAN-bus-funktionalitet?](#vil-du-implementere-widgets-til-loconet--og-can-bus-funktionalitet)
    *   [Jeg har fundet en fejl i Z21Dashboard, hvad gør jeg?](#jeg-har-fundet-en-fejl-i-z21dashboard-hvad-gør-jeg)
    *   [Jeg synes Z21Dashboard er smart men jeg er ikke stærk i netværk etc. så hvad gør jeg?](#jeg-synes-z21dashboard-er-smart-men-jeg-er-ikke-stærk-i-netværk-etc-så-hvad-gør-jeg)

# Indledning

Velkommen til Z21Dashboard. Denne vejledning hjælper dig med at komme i gang med at bruge programmet til at overvåge
og styre din modeljernbane via din Roco/Fleischmann Z21 centralstation.

## Hvorfor Z21Dashboard?

Z21Dashboard startede som et "for sjov projekt". Jeg er IT-udvikler, og syntes det kunne sjovt at
skrive et program til min z21Start, så jeg kunne se hvilke lokomotiver og sporskifter jeg styrede på
min togbane.

Så kom alle ideérne og så blev Z21Dashboard til.

## Lidt om teknikken

Z21Dashboard er Open Source software, og det findes i mit Github repository, hvor både kildekode og
binære filer er tilgængelig.

Z21Dashboard er udviklet i C# og .NET 9. Det er udviklet som en .NET MAUI Blazor Hybrid
applikation.

# Z21 familien af centralstationer

Du kan forbinde Z21Dashboard til hele familien af 21 centralstationer fra Roco:

* z21
* z21Start
* Z21
* Z21 XL

I denne vejledning bruges termen "Z21" om dem alle, med mindre der er noget særligt for en specifik model.

## Brug af protokol

Normalt forbindes Z21 centralstationerne med styring af 2-skinne modeltogsbaner og protokollen
DCC. Z21 centralstationerne er af typen multiprotokol, da der tillades at benytte både DCC
og Märklin Motorola. mfx er ikke understøttet af Z21 centralstationerne.

Fra ny er Z21 centralstationen sat op til at benytte både DCC og Märklin Motorola. Du eller andre
kan have ændret dette i Z21 ved hjælp af Rocos applikation "Maintenance Tool", hvor man kan 
angive hvilke protokoller den skal understøttet.

Selvom du kan skifte mellem protokoller i visse af Z21Dashboards indstilligner, kan dette
ikke overskrive indstillingen i Z21. Dvs. hvis Z21 er indstillet til kun DCC, så ænderer det ikke
protokollen for et lokomotiv eller sporskifte, at du vælger Märklin Motorola i Z21Dashboard.

## z21/z21Start og låst eller ej

Generelt er z21 (hvid kasse, hvor teksten på fronten er "z21") låst op, dog blev der tidligere produceret z21 som
var låst, ligesom z21Start (hvid kasse, hvor teksten på fronten er "z21Start") er det. Der kan købes to forskellige
Roco varer med oplåstningskoder, hhv. varenr. 10814 (wifi router og oplåsningskode) og 10818 (oplåsningskode).

Er din z21/z21Start låst, vil det ikke være muligt at styre lokomotiver og sporskifter i Z21Dashboard. Du kan dog
sagtens se tilstanden på dem, i takt med at du styrer dem med din multiMAUS.

# Første gang du starter

Når du åbner Z21Dashboard vises widget’en ”Forbindelse” med den senest anvendte IP-adresse i feltet. Første gang
du åbner Z21Dashboard står Z21s standard IP-adresse (192.168.0.111) i feltet. Har du ændret IP-adressen i din Z21
centralstation, skal du indtaste den IP-adresse. Når du klikker på knappen "Forbind" bliver IP-adressen gemt og
vil stå i feltet næste gang du starter Z21Dashboard.

1.	Find Forbindelses-vinduet: Øverst på skærmen vil du se et vindue med titlen "Forbindelse"
2.	Indtast IP-adresse: I feltet er der allerede indtastet en standard-IP-adresse (192.168.0.111). Hvis din Z21 har
en anden adresse på dit netværk, skal du rette den. Programmet husker den adresse, du indtaster, til næste gang.
3.	Klik på den grønne "Connect"-knap
4.	Hvis Z21Dashboard opnår forbindelse til din Z21, vil status-mærket i øverste højre hjørne af vinduet skifte
til "Forbundet" og blive grønt. Resten af dine dashboard-vinduer vil nu blive vist.

I mens Z21Dashboard forsøger at forbinde til din Z21 centralstation, vil der stå "Forbinder" i det lille grå felt, 
øverst til højre i Forbind widget. Hvis der ikke opnås forbindelse, vil der efter nogle sekunder igen stå "Ej
forbundet".

# Dashboardet

Hele området under "Connection"-vinduet er dit personlige lærred. Her kan du arrangere de forskellige
informations- og kontrolvinduer (widgets), præcis som du ønsker det.

## Flyt rundt på vinduer

Det er nemt at flytte et vindue:
1.	Find headeren: Peg med musen på overskriftslinjen i det vindue, du vil flytte
(f.eks. på teksten "Power Monitor"). Din musemarkør vil ændre sig til en "gribe-hånd".
2.	Klik og Træk: Klik og hold museknappen nede, og træk vinduet til en ny position på skærmen.
3.	Slip: Slip museknappen for at placere vinduet. Programmet gemmer automatisk dine vinduers positioner,
så dit layout ser ens ud, næste gang du starter.

## Tilpas dit dashboard (vis/skjul vinduer)

Du behøver ikke have alle vinduer vist på én gang. Du kan nemt tænde og slukke for dem, du har brug for.
1.	Åbn Indstillinger: Klik på det store tandhjuls-ikon (⚙️) øverst i højre hjørne af programvinduet. En
popup-menu ved navn "Settings" vil åbne sig.
2.	Tænd/Sluk: I menuen ser du en liste over alle tilgængelige vinduer med en tænd/sluk-knap ud for hver. Klik
på knappen for at vise eller skjule et vindue. Ændringen sker med det samme på dit dashboard i baggrunden.
3.	Luk Indstillinger: Klik på "Close" eller krydset for at lukke indstillingsmenuen.

## De vigtigste vinduer forklaret

### Forbindelses-vinduet
Dette er dit kontrolpanel til forbindelsen. Herfra kan du:
* Oprette og afbryde forbindelsen til din Z21
* Tænde og slukke for skinnestrømmen med "Banestrøm til" og "Banestrøm af"
* Aktivere Nødstop for at stoppe lokomotiver

Vær opmærkom på, at Nødstop er implementeret forskelligt i dekoderne. I nogle betyder det at lokomotivet stopper
med det samme, og i andre betyder det at hastigheden sættes til 0 mens bremse-forsinkelse er stadig aktiv. Det kan anbefales
at du tester dine lokomotiver så du ved, om du reelt kan brugt Nødstop.

### Lokomotivstyring
Dette er dit digitale kørekontrol for et enkelt lokomotiv.
1.	Vælg et Lokomotiv: Indtast adressen på det lokomotiv, du vil se eller styre, i "Adresse"-feltet.
2.	Se Live Status: Så snart du har indtastet en adresse, vil vinduet vise lokomotivets nuværende hastighed,
kørselsretning og hvilke funktioner (F0-F31), der er aktive. Denne information opdateres live, også hvis du styrer
lokomotivet fra en anden controller (f.eks. en MultiMAUS).
3.	Styr Lokomotivet: Hvis din z21/z21Start er "låst op" (se andet sted), kan du bruge slideren til at ændre
hastigheden, klikke på "Fremad"/"Baglæns" for at skifte retning, og klikke på F-knapperne for at tænde/slukke for
funktioner.

Når du har valgt et lokomotiv (adressefelt ikke er 0), kan du klikke på tandhjulet øverst til højre. Nu vises der
et popup vinudet. Mulighederne i dette vindue afhænger af om din z21/z21Start er låst.

Låst eller ej, så kan du:
* Give lokomotivet et navn. Dette navn vises herefter i Z21Dashboard sammen med adressen.
* Angive et antal driftstimer før der skal laves service på lokomotivet.
* Se hvor lang tid der er til der skal udføres service
* Nulstille service-tidstællerne, når du har udført serviec.

Ej låst:
* Vælge protokol og antallet af hastighedstrin

Låst:
* Vælge protokol. Vil du ændre antallet af hastighedstrin, skal du gøre det på din multiMAUS.

Bemærk, hvis du har valgt Märklin Motorola version 1 protokollen, så vil kun
funktionsknappen for lys være aktiv. Har du valgt Märklin Motorola version 2, så
vil funktionskanppen for lys samt F1 - F4 være aktive.

### Lokomotivers driftstid
Dette vindue giver et fantastisk overblik over de lokomotiver, der er i brug. Det viser en liste over alle aktive
lokomotiver og holder styr på, hvor længe hvert enkelt har været i drift. Det er også her, du kan give dine
lokomotiver personlige navne.

Ud for hvert lokomotiv er en knap med et tandhjul. Dette åbner dialogen med indstillinger for det pågældende 
lokomotiv. Når du hMulighederne i dette vindue afhænger af om din z21/z21Start er låst.

Låst eller ej, så kan du:
* Give lokomotivet et navn. Dette navn vises herefter i Z21Dashboard sammen med adressen.
* Angive et antal driftstimer før der skal laves service på lokomotivet. I oversigten vil driftstiden have gul
baggrund når der er mindre end en time til service skal udføres. Baggrunden blier rød når service ikke er udført.
* Se hvor lang tid der er til der skal udføres service
* Nulstille service-tidstællerne, når du har udført serviec.

Ej låst:
* Vælge protokol og antallet af hastighedstrin

Låst:
* Vælge protokol. Vil du ændre antallet af hastighedstrin, skal du gøre det på din multiMAUS.

Bemærk, at drifstiden gemmes på din pc når du afslutter programmet. Så næste gang
starter programmet og forbinder til din Z21, fortsættes optællingen af drifttiden.

Driftstid tæller når hastigheden er forskellig fra 0.

### Sporskifter

Dette vindue viser de sporskifter, hvor du har skiftet deres potition med din multiMAUS. 
Ud for hvert sporskifte vises der en knap for indstillinger. Når du klikker på den, åbnes en
popup dialog, der lader dig vælge hviklen protokol der skal benyttes til dekoderen i 
sporskiftet.

I popup kan du vælge mellem "DCC" og "MM" for Märklin Motorola.

### Spænding og strøm oversigt

Dette vindue viser en graf med to linjer; en for spænding og en for strøm, således at du
kan følge hvordan det udvikler sig over tid.

Ved at klikke på knappen med tandhjulet, øverst til højre widget'en, kan du ændre det interval
som grafen opdateres med.

Uanset hvilket interval du vælger, så modtager Z21Dashboard opdateringer løbende fra Z21 centralstationen.
Widget'en husker den højeste værdi modtaget, og når intervallet er "udløbet" skrives den højest
værdi til grafen.

# FAQ (OSS)

## Hvad betyder "Låst" (Locked)?

Hvis du har en z21start (den hvide model), kan den være "låst" fra fabrikken. Det betyder, at
den ikke tillader kørsel og sporskifte via netværk.
* Hvis LocoControl viser "Låst": Du kan stadig bruge Z21 Dashboard som et overvågningsværktøj.
Du kan se live status for dine lokomotiver, men du kan ikke styre dem fra programmet (knapperne vil være deaktiverede). Du kan dog skifte protokol for lokomotover
* Hvis LocoControl viser "Låst op": Alle funktioner i programmet er tilgængelige, og du kan styre dine lokomotiver direkte.
For at låse din z21start op, skal du bruge en oplåsningskode fra Roco (varenummer 10818 eller 10814).

## Kan navne fra min multiMAUS eller Z21 app overføres til Z21Dashboard?

Det kan desværre ikke lade sig gøre.

## Z21Dashboard kan ikke forbinde til min Z21

Det åbenlyse, tjek at IP-adressen er korrekt angivet. Åben et kommandoprompt i Windows, og
ping din Z21. Hvis ping ikke får noget svar, så er der et netværksproblem, du skal have løst
først.

Første gang du starter Z21Dashboard, vil Windows' firewall spørge om det er ok, at Z21Dashboard
kommunikerer ud på netværket. Dette skal der svares Ja til. Fik du svaret Nej, så må du ind i 
Windows firewall og give tilladelse til, at Z21Dashboard må kommunikere ud af din pc.

## Vil du implementere widgets til LocoNet- og CAN-bus-funktionalitet?

Det korte svar er nej. Det lange svar er, at jeg ejer en z21Start, og derfor har jeg ikke behovet for sådanne
widgets, og jeg kan ikke teste funktionaliteten.

## Jeg har fundet en fejl i Z21Dashboard, hvad gør jeg?

Gå ind på repositoriet, og opret et Issue.

### Jeg har ikke en konto på GitHub

På nuvæende tidspunkt er projektets hjemsted mit repository på GitHub, og det er her
dialogen om Z21Dashboard skal være.

## Jeg synes Z21Dashboard er smart men jeg er ikke stærk i netværk etc. så hvad gør jeg?

Du må søge hjælp hos en ven eller eventuelt i din modeltogsklub, for at få den nødvendige hjælp
til at få opsat netværk og eventuelt din router.
