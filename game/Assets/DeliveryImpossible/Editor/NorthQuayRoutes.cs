using DeliveryImpossible.Interaction;
using DeliveryImpossible.Orders;
using UnityEditor;
using UnityEngine;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static CampaignLevel BuildHarbor(Transform hub)
        {
            Transform quay = Location(hub, "01 Ferry Terminal", 0f);
            Material timber = QuaySurface("Salt Worn Timber", "DockWood", "#D2CABC", 1.62f);
            Material concrete = QuaySurface("Berth Concrete", "IndustrialFloor", "#C8CDCE", 2.35f);
            Material steel = Material("Harbor Rail", "#3E5257");
            Material amber = Material("Quay Signal Amber", "#DF9E53", emissive: true);
            Material sea = OpenWater();

            Block(quay, "Outer harbor water", new Vector3(0f, -1.44f, 40f), new Vector3(620f, 0.16f, 670f), sea);
            const float nearEdge = 13f;
            const float farEdge = 23.10f;
            const float railOffset = 2.56f;
            Floor(quay, timber, 16f, -3f, nearEdge);
            Floor(quay, timber, 20f, farEdge, 71f);
            Block(quay, "Pier foundation A", new Vector3(0f, -0.78f, 5.0f), new Vector3(16f, 0.97f, 16f), concrete);
            Block(quay, "Pier foundation B", new Vector3(0f, -0.78f, 47.05f), new Vector3(20f, 0.97f, 47.9f), concrete);
            Guardrail(quay, new Vector3(-7.84f, 0f, -2.8f), new Vector3(7.84f, 0f, -2.8f), steel);
            Guardrail(quay, new Vector3(-9.83f, 0f, 70.75f), new Vector3(9.83f, 0f, 70.75f), steel);

            foreach (float side in new[] { -1f, 1f })
            {
                HarborRail(quay, side * 7.84f, -2.8f, nearEdge, steel);
                HarborRail(quay, side * 9.83f, farEdge, 70.75f, steel);
                Guardrail(quay, new Vector3(side * railOffset, 0f, nearEdge), new Vector3(side * 7.84f, 0f, nearEdge), steel);
                Guardrail(quay, new Vector3(side * railOffset, 0f, farEdge), new Vector3(side * 9.83f, 0f, farEdge), steel);
                for (float z = 27.7f; z < 68.8f; z += 8.3f)
                {
                    Cylinder(quay, "Quay light pole", new Vector3(side * 9.03f, 2.22f, z), 0.105f, 4.44f, steel, Quaternion.identity, solid: true);
                    RoundedBlock(quay, "Quay light", new Vector3(side * 8.78f, 4.39f, z),
                        new Vector3(0.73f, 0.12f, 0.36f), amber);
                    Lamp(quay, new Vector3(side * 8.7f, 4.22f, z), new Color(1f, 0.75f, 0.49f), 8.1f, 2.8f);
                }
            }

            BerthScenery(quay, steel);
            Transform dispatch = Child(quay, "Dispatch and lighthouse district");
            dispatch.localPosition = new Vector3(-0.8f, 0, 24.1f);
            BuildPortOffice(dispatch, steel, amber, timber, concrete);

            Transform bridge = Child(quay, "Motorized bridge pivot");
            bridge.localPosition = new Vector3(0f, 0f, nearEdge);
            float span = farEdge - nearEdge;
            Block(bridge, "Bridge deck", new Vector3(0f, -0.18f, span * 0.5f), new Vector3(5.32f, 0.36f, span), concrete, true);
            foreach (float x in new[] { -railOffset, railOffset })
                HarborRail(bridge, x, 0.10f, span - 0.10f, steel);
            for (float z = 0.51f; z < span; z += 0.69f)
                Block(bridge, "Anti-slip strip", new Vector3(0f, 0.014f, z), new Vector3(4.95f, 0.018f, 0.075f), amber);
            bridge.localRotation = Quaternion.Euler(-76f, 0f, 0f);
            foreach (float x in new[] { -2.95f, 2.95f })
            {
                Cylinder(quay, "Bridge motor axis", new Vector3(x, 0.31f, 12.99f), 0.68f, 0.49f, steel, Quaternion.Euler(0, 0, 90), solid: true);
                CoursePost(quay, "МОСТ", new Vector3(x * 1.22f, 0f, 11.86f), amber, steel);
            }

            DeliveryCheckpoint left = BridgePedal(quay, -2.75f, amber, steel);
            DeliveryCheckpoint right = BridgePedal(quay, 2.75f, amber, steel);
            DeliveryCourse course = Course(quay, DeliveryMechanic.Drawbridge, data =>
            {
                data.FindProperty("bridgeDeck").objectReferenceValue = bridge.gameObject;
                SetObjects(data.FindProperty("controls"), new[] { left, right });
            });

            var parcel = GameObject.Find("Test Network Cargo").GetComponent<NetworkGrabbable>();
            parcel.transform.SetPositionAndRotation(new Vector3(-0.15f, 0.59f, 3.05f), Quaternion.identity);
            PrefabUtility.RecordPrefabInstancePropertyModifications(parcel.transform);
            MarkPickup(quay, new Vector3(-0.15f, 0.018f, 3.05f), 1.42f, 1.42f, amber);
            NetworkGrabbable buoy = PutCargo(quay, MakeSpecialCargo("HarborBuoy"), new Vector3(3.4f, 0.66f, 2.65f), true, amber);
            RouteStuds(quay, amber, new Vector2(0f, 24f), new Vector2(0f, 34f), new Vector2(4.45f, 47f),
                new Vector2(4.45f, 55.2f), new Vector2(2.15f, 63.5f));
            return new CampaignLevel
            {
                Name = "ПАРОМНЫЙ ТЕРМИНАЛ", Hint = "Доставьте почту через досмотр к диспетчеру у маяка.",
                Course = course, Spawn = SpawnAt(quay, Vector3.zero), MainCargo = parcel, BonusCargo = buoy,
                MainLabel = "Почтовая посылка", BonusLabel = "Сигнальный буй",
                Checkpoints = new[]
                {
                    Volume(quay, "Bridge exit", new Vector3(0f, 1.26f, 24.65f), new Vector3(5.35f, 2.54f, 2.3f)),
                    Volume(quay, "Quay inspection", new Vector3(4.45f, 1.38f, 52.2f), new Vector3(5.8f, 2.8f, 2.75f))
                },
                MainZone = ZoneAt(quay, new Vector3(2.12f, 0, 62.9f), 5.6f)
            };
        }

        private static DeliveryCheckpoint BridgePedal(Transform quay, float x, Material glow, Material steel)
        {
            Vector3 point = new Vector3(x, 0f, 9.15f);
            Block(quay, "Pressure plate base", point + new Vector3(0f, 0.028f, 0f), new Vector3(1.80f, 0.056f, 1.80f), steel, true);
            MarkPickup(quay, point + Vector3.up * 0.065f, 1.69f, 1.69f, glow);
            ImportedProp("ControlPedal", false, quay, new Vector3(1.54f, 0.16f, 1.54f),
                point + new Vector3(0f, 0.07f, 0f));
            return Volume(quay, "Bridge pedal sensor", point + Vector3.up * 0.33f, new Vector3(1.72f, 0.84f, 1.72f));
        }

        private static CampaignLevel BuildFoundry(Transform hub)
        {
            Transform hall = Location(hub, "02 Port Sorting Hall", 135f);
            Material floor = QuaySurface("Sorting Floor", "IndustrialFloor", "#B7B8B2", 2.1f);
            Material brick = QuaySurface("Sorting Brickwork", "QuayBrick", "#AAA7A0", 2.65f);
            Material iron = QuaySurface("Sorting Sheetwork", "SteelPanels", "#787F80", 2.65f);
            Material safety = Material("Industrial Waymark", "#E1AA5F");
            Material charcoal = Material("Industrial Steel Frame", "#323842");
            IndustrialShell(hall, floor, brick, iron, 20f, 76f, 6.9f, clerestory: true);
            DetailSortingHall(hall, charcoal, iron, safety);
            for (int bay = 0; bay < 7; bay++)
            {
                float z = 5.1f + bay * 10.4f;
                ImportedProp(bay % 2 == 0 ? "Hopper" : "Machine", false, hall,
                    new Vector3(2.5f, 3.3f, 2.7f), new Vector3(-8.0f, 1.65f, z), 90f, solid: true);
                ImportedProp(bay % 2 == 0 ? "Robot" : "Machine", false, hall,
                    new Vector3(2.5f, 3.3f, 2.7f), new Vector3(8.05f, 1.65f, z), -90f, solid: true);
            }

            Block(hall, "Continuous conveyor chassis", new Vector3(0f, 0.027f, 20.35f),
                new Vector3(2.92f, 0.055f, 23.2f), charcoal, true);
            foreach (float x in new[] { -1.45f, 1.45f })
                Block(hall, "Continuous conveyor guide", new Vector3(x, 0.105f, 20.35f),
                    new Vector3(0.105f, 0.21f, 23.2f), safety, true);
            for (float z = 8.97f; z < 31.87f; z += 0.44f)
                Cylinder(hall, "Steel belt roller", new Vector3(0f, 0.053f, z), 0.088f, 2.80f, iron, Quaternion.Euler(0f, 0f, 90f));

            const float wallZ = 25.55f;
            Partition(hall, brick, -10.0f, -1.54f, 0f, 6.9f, wallZ);
            Partition(hall, brick, -1.54f, 1.54f, 1.10f, 6.9f, wallZ);
            Partition(hall, brick, 1.54f, 3.22f, 0f, 6.9f, wallZ);
            Partition(hall, brick, 3.22f, 6.22f, 2.91f, 6.9f, wallZ);
            Partition(hall, brick, 6.22f, 10.0f, 0f, 6.9f, wallZ);
            Renderer gate = Block(hall, "Roll-up personnel gate", new Vector3(4.72f, 1.47f, wallZ - 0.195f),
                new Vector3(3.09f, 2.96f, 0.22f), iron, true);
            CourseLabel(hall, "КОНТРОЛЬ ЛЕНТЫ", new Vector3(0, 1.53f, wallZ - 0.25f), 3.4f, safety, charcoal);
            CourseLabel(hall, "ПРОХОД ПОСЛЕ СКАНИРОВАНИЯ", new Vector3(4.72f, 3.48f, wallZ - 0.27f), 4.03f, safety, charcoal);
            Block(hall, "Personnel route paint", new Vector3(4.72f, 0.014f, 21.1f),
                new Vector3(0.52f, 0.012f, 18.45f), safety);
            Transform loading = Child(hall, "Conveyor loading point");
            loading.localPosition = new Vector3(0f, 0.062f, 10.35f);
            MarkPickup(hall, loading.localPosition + Vector3.up * 0.04f, 2.66f, 2.32f, safety);
            CoursePost(hall, "ЗАГРУЗКА ЛЕНТЫ", new Vector3(-2.60f, 0f, 9.8f), safety, charcoal);
            var belt = Volume(hall, "Driven conveyor section", new Vector3(0f, 0.69f, 20.35f), new Vector3(2.71f, 1.5f, 23.2f));

            RouteStuds(hall, safety, new Vector2(0f, 33.1f), new Vector2(-2.8f, 46f), new Vector2(-2.8f, 56f), new Vector2(0f, 69f));
            CheckArch(hall, new Vector3(-2.8f, 0, 48f), 5.4f, charcoal, safety, "УЧЁТ ПАРТИИ");
            CargoStack(hall, new Vector3(0.5f, 0f, 43f), brick, charcoal);
            CargoStack(hall, new Vector3(-5.7f, 0f, 56f), brick, charcoal);
            CourseLabel(hall, "ОТГРУЗКА", new Vector3(0, 3.0f, 74.7f), 5.4f, safety, charcoal);

            DeliveryCourse course = Course(hall, DeliveryMechanic.Conveyor, data =>
            {
                data.FindProperty("belt").objectReferenceValue = belt;
                data.FindProperty("beltLoadPoint").objectReferenceValue = loading;
                data.FindProperty("bypassGate").objectReferenceValue = gate.transform;
            });
            return new CampaignLevel
            {
                Name = "СОРТИРОВОЧНЫЙ ЦЕХ", Hint = "Заберите груз после ленты и довезите его через учёт к отгрузке.",
                Course = course, Spawn = SpawnAt(hall, Vector3.zero),
                MainCargo = PutCargo(hall, MakeSpecialCargo("FoundryGearbox"), new Vector3(-1.5f, 0.44f, 3.25f), false, safety),
                BonusCargo = PutCargo(hall, MakeSpecialCargo("RollerCassette"), new Vector3(2.85f, 0.39f, 2.95f), true, safety),
                MainLabel = "Лебёдочный редуктор", BonusLabel = "Кассета роликов",
                Checkpoints = new[]
                {
                    Volume(hall, "Belt exit scanner", new Vector3(0f, 0.60f, 28.85f), new Vector3(2.76f, 1.22f, 1.84f)),
                    Volume(hall, "Batch inspection", new Vector3(-2.8f, 1.44f, 48f), new Vector3(5.65f, 2.95f, 2.8f))
                },
                MainZone = ZoneAt(hall, new Vector3(0, 0, 69.3f), 5.75f)
            };
        }

        private static CampaignLevel BuildArchive(Transform hub)
        {
            Transform archive = Location(hub, "03 Quayside Archive", 270f);
            Material floor = QuaySurface("Archive Cement", "IndustrialFloor", "#E0D8C4", 2.35f);
            Material brick = QuaySurface("Archive Brick", "QuayBrick", "#B9A8A0", 3.5f);
            Material panel = QuaySurface("Archive Oak", "DockWood", "#9D8775", 2.15f);
            Material railing = Material("Archive Painted Steel", "#443B3B");
            Material brass = Material("Archive Brass", "#B89B66");
            Material red = Material("Safety Beam Red", "#F33C36", emissive: true);
            IndustrialShell(archive, floor, brick, panel, 20f, 81f, 6.3f);
            DetailPortArchive(archive, panel, railing, brass);
            foreach (float side in new[] { -1f, 1f })
            for (int index = 0; index < 6; index++)
            {
                float z = 7.8f + index * 12.1f;
                Vector3 point = new Vector3(side * 8.15f, 0, z);
                CargoStack(archive, point, panel, railing);
                Block(archive, "Archive storage bay", point + new Vector3(0f, 1.61f, 1.8f),
                    new Vector3(1.92f, 3.22f, 0.37f), panel, true);
                CourseLabel(archive, $"СТЕЛЛАЖ {index + 1:00}", point + new Vector3(0f, 2.49f, 1.58f), 1.7f, brass, railing);
                if (index == 2 && side < 0)
                    GalleryExhibit(archive, point + Vector3.forward * 3.1f, panel, floor, brass);
            }

            DeliveryCheckpoint one = TimedScanner(archive, 1, -2.95f, 17.9f, brick, brass, red, out GameObject beamsA);
            DeliveryCheckpoint two = TimedScanner(archive, 2, 3.25f, 38.8f, brick, brass, red, out GameObject beamsB);
            DeliveryCheckpoint three = TimedScanner(archive, 3, -2.1f, 59.35f, brick, brass, red, out GameObject beamsC);
            RouteStuds(archive, brass, new Vector2(-2.95f, 12f), new Vector2(-2.95f, 20.3f),
                new Vector2(3.25f, 34.2f), new Vector2(3.25f, 41.5f), new Vector2(-2.1f, 55.6f),
                new Vector2(-2.1f, 61.9f), new Vector2(0f, 72.1f));
            CourseLabel(archive, "ХРАНИЛИЩЕ · ПРИЁМ ЭКСПОНАТОВ", new Vector3(0, 2.9f, 79.4f), 6.3f, brass, railing);
            DeliveryCourse course = Course(archive, DeliveryMechanic.Security, data =>
                SetObjects(data.FindProperty("securityBeams"), new[] { beamsA, beamsB, beamsC }));
            return new CampaignLevel
            {
                Name = "ПОРТОВЫЙ АРХИВ", Hint = "Перенесите экспонат в хранилище, когда сканеры гаснут.",
                Course = course, Spawn = SpawnAt(archive, Vector3.zero),
                MainCargo = PutCargo(archive, MakeSpecialCargo("MuseumVase"), new Vector3(-1.42f, 0.61f, 3.15f), false, brass),
                BonusCargo = PutCargo(archive, MakeSpecialCargo("FramedPainting"), new Vector3(2.7f, 0.69f, 2.98f), true, brass),
                MainLabel = "Фарфоровый экспонат", BonusLabel = "Карта старой гавани",
                Checkpoints = new[] { one, two, three },
                MainZone = ZoneAt(archive, new Vector3(0f, 0f, 73.8f), 5.85f)
            };
        }

        private static DeliveryCheckpoint TimedScanner(Transform parent, int index, float x, float z,
            Material brick, Material brass, Material light, out GameObject beams)
        {
            const float gap = 2.12f;
            Partition(parent, brick, -10f, x - gap, 0f, 6.3f, z);
            Partition(parent, brick, x + gap, 10f, 0f, 6.3f, z);
            Partition(parent, brick, x - gap, x + gap, 3.14f, 6.3f, z);
            ImportedProp("Scanner", false, parent, new Vector3(0.63f, 2.6f, 0.81f),
                new Vector3(x + gap + 0.45f, 1.3f, z - 0.53f), solid: true);
            CourseLabel(parent, $"СКАНЕР {index:00}", new Vector3(x, 3.47f, z - 0.255f), 2.8f, brass, brick);
            Block(parent, "Scanner approaching stripe", new Vector3(x, 0.018f, z - 1.02f),
                new Vector3(4.16f, 0.018f, 0.14f), brass);

            Transform rays = Child(parent, $"Scanner rays {index}");
            for (int row = 0; row < 7; row++)
            {
                Block(rays, "Red sensor light", new Vector3(x, 0.16f + row * 0.45f, z),
                    new Vector3(gap * 2f, 0.025f, 0.035f), light);
            }

            beams = rays.gameObject;
            return Volume(parent, "Timed scanner crossing", new Vector3(x, 1.45f, z), new Vector3(gap * 2.01f, 2.90f, 0.90f));
        }

        private static CampaignLevel BuildColdStore(Transform hub, GameObject thermal)
        {
            Transform cold = Location(hub, "04 Refrigerated Terminal", 399f);
            Material floor = QuaySurface("Cold Quay Flooring", "IndustrialFloor", "#CED1D6", 2.13f);
            Material panel = QuaySurface("Cold Room Steel", "SteelPanels", "#9CC4CA", 2.54f);
            Material roof = QuaySurface("Cold Terminal Roof", "SteelPanels", "#687D89", 2.8f);
            Material cyan = Material("Cold Station Light", "#78E1EA", emissive: true);
            Material baseMetal = Material("Cold Store Trim", "#315861");
            IndustrialShell(cold, floor, panel, roof, 22f, 92f, 6.45f, refrigeration: true);
            DetailRefrigerationHall(cold, panel, baseMetal);
            for (int bay = 0; bay < 8; bay++)
            {
                float z = 6.8f + bay * 10.7f;
                float x = bay % 2 == 0 ? -9.0f : 9.0f;
                ImportedProp("PipeValve", false, cold, new Vector3(2.15f, 2.78f, 2.2f),
                    new Vector3(x, 1.39f, z), bay % 2 * 180f, solid: true);
                RoundedBlock(cold, "Insulated pallet bin", new Vector3(-x, 0.70f, z + 2.54f),
                    new Vector3(2.15f, 1.4f, 3.1f), panel, solid: true);
                Block(cold, "Insulated pallet bin lid", new Vector3(-x, 1.44f, z + 2.54f),
                    new Vector3(2.23f, 0.13f, 3.21f), baseMetal, true);
            }

            Vector3[] bays =
            {
                new Vector3(-2.6f,0,11.7f), new Vector3(2.65f,0,31.55f),
                new Vector3(-2.8f,0,51.92f), new Vector3(2.25f,0,72.35f)
            };
            var stations = new DeliveryCheckpoint[bays.Length];
            for (int index = 0; index < bays.Length; index++)
                stations[index] = ColdBay(cold, bays[index], index + 1, baseMetal, cyan);
            RouteStuds(cold, cyan, new Vector2(-0.75f, 6f), new Vector2(-2.6f, 9f));
            CourseLabel(cold, "ОТГРУЗКА ОБРАЗЦОВ", new Vector3(-1.9f, 3.1f, 89.8f), 5.6f, cyan, baseMetal);
            DeliveryCourse course = Course(cold, DeliveryMechanic.ColdChain, data =>
                SetObjects(data.FindProperty("controls"), stations));
            return new CampaignLevel
            {
                Name = "ХОЛОДИЛЬНЫЙ СКЛАД", Hint = "На каждой станции дождитесь снижения нагрева до 23%.",
                Course = course, Spawn = SpawnAt(cold, Vector3.zero),
                MainCargo = PutCargo(cold, thermal, new Vector3(-1.2f, 0.43f, 3.07f), false, cyan),
                BonusCargo = PutCargo(cold, MakeSpecialCargo("IceSpecimen"), new Vector3(3.05f, 0.60f, 2.84f), true, cyan),
                MainLabel = "Термоконтейнер", BonusLabel = "Контрольная проба",
                Checkpoints = stations, MainZone = ZoneAt(cold, new Vector3(-1.9f, 0f, 85.5f), 5.75f)
            };
        }

        private static DeliveryCheckpoint ColdBay(Transform parent, Vector3 point, int number, Material frame, Material glow)
        {
            MarkPickup(parent, point + Vector3.up * 0.026f, 4.35f, 4.25f, glow);
            foreach (float side in new[] { -1f, 1f })
            {
                RoundedBlock(parent, "Cooling column", point + new Vector3(side * 2.25f, 1.52f, 0.95f),
                    new Vector3(0.38f, 3.03f, 0.38f), frame, solid: true);
                RoundedBlock(parent, "Cooling indicator", point + new Vector3(side * 2.25f, 1.51f, 0.734f),
                    new Vector3(0.13f, 2.34f, 0.075f), glow);
            }

            RoundedBlock(parent, "Chiller bank", point + new Vector3(0f, 3.05f, 0.95f),
                new Vector3(4.92f, 0.38f, 0.89f), frame, solid: true);
            CourseLabel(parent, $"ОХЛАЖДЕНИЕ {number:00}", point + new Vector3(0f, 3.05f, 0.48f), 3.26f, glow, frame);
            Lamp(parent, point + new Vector3(0f, 2.88f, 0.34f), new Color(0.48f, 0.91f, 1f), 5.4f, 3.8f);
            return Volume(parent, "Refrigerated bay", point + Vector3.up * 1.42f, new Vector3(4.33f, 2.87f, 4.14f));
        }

        private static CampaignLevel BuildPowerStation(Transform hub, GameObject module)
        {
            Transform station = Location(hub, "05 Tide Gate Power Station", 535f);
            Material floor = QuaySurface("Station Concrete", "IndustrialFloor", "#BEC6C5", 2.05f);
            Material wall = QuaySurface("Station Sheet Metal", "SteelPanels", "#A0AEB3", 2.53f);
            Material brick = QuaySurface("Pump House Brick", "QuayBrick", "#96928C", 3.2f);
            Material dark = Material("Pump House Steel", "#343F4D");
            Material deck = QuaySurface("Service Bridge Deck", "SteelPanels", "#657D84", 1.7f);
            Material amber = Material("Hydro Warning Light", "#FCC075", emissive: true);
            Material light = Material("Pump Panel Mint", "#8DD2C0", emissive: true);
            Block(station, "Tidal basin", new Vector3(0f, -1.47f, 48f), new Vector3(570f, 0.08f, 630f), OpenWater());
            BuildCoastalVista(station, powerStation: true);
            const float nearEdge = 17.2f;
            const float farEdge = 26.4f;
            const float leftRail = 2.085f;
            const float rightRail = 6.11f;
            Floor(station, floor, 15f, -3f, nearEdge);
            Floor(station, floor, 20f, farEdge, 76.05f);
            Block(station, "Service footbridge floor", new Vector3(4.1f, -0.15f, (nearEdge + farEdge) * 0.5f),
                new Vector3(4.25f, 0.30f, farEdge - nearEdge), deck, true);
            HarborRail(station, leftRail, nearEdge, farEdge, dark);
            HarborRail(station, rightRail, nearEdge, farEdge, dark);
            foreach (float x in new[] { -7.32f, 7.32f }) HarborRail(station, x, -2.85f, nearEdge, dark);
            Guardrail(station, new Vector3(-7.32f, 0f, -2.85f), new Vector3(7.32f, 0f, -2.85f), dark);
            Guardrail(station, new Vector3(-7.32f, 0f, nearEdge), new Vector3(leftRail, 0f, nearEdge), dark);
            Guardrail(station, new Vector3(rightRail, 0f, nearEdge), new Vector3(7.32f, 0f, nearEdge), dark);
            Guardrail(station, new Vector3(-9.72f, 0f, farEdge), new Vector3(leftRail, 0f, farEdge), dark);
            Guardrail(station, new Vector3(rightRail, 0f, farEdge), new Vector3(9.72f, 0f, farEdge), dark);
            CoursePost(station, "СЕРВИСНЫЙ МОСТ", new Vector3(5.82f, 0, 14.1f), amber, dark);
            foreach (float x in new[] { -9.72f, 9.72f }) HarborRail(station, x, farEdge, 76f, dark);

            DeliveryCheckpoint[] checks = BuildSubstationStages(station, deck, wall, brick, dark, amber, light);
            DeliveryZone main = ZoneAt(station, new Vector3(-4.2f, SubstationDeckHeight, 92.4f), 4.7f, 90f);
            DeliveryZone backup = ZoneAt(station, new Vector3(4.2f, SubstationDeckHeight, 92.4f), 4.7f, -90f);
            Arrow(main.transform, new Vector3(0, 0.047f, 0), light, 1.55f);
            Arrow(backup.transform, new Vector3(0, 0.047f, 0), amber, 1.55f);
            RouteStuds(station, light, new Vector2(0f, 3.8f), new Vector2(0f, 8.1f));
            RouteStuds(station, light, new Vector2(1.0f, 11.9f), new Vector2(4.1f, 14.9f),
                new Vector2(4.1f, 29f), new Vector2(-2.95f, 38.05f));
            RouteStuds(station, light, new Vector2(-2.95f, 42.55f), new Vector2(2.95f, 51.95f));
            RouteStuds(station, light, new Vector2(2.95f, 56.45f), new Vector2(0f, 60.3f));
            RouteStuds(station, light, SubstationDeckHeight, new Vector2(0f, 76.25f), new Vector2(0f, 79.4f));
            return new CampaignLevel
            {
                Name = "ПРИЛИВНАЯ ПОДСТАНЦИЯ", Hint = "Запустите подстанцию. Контактная площадка перед мостом.",
                Course = Course(station, DeliveryMechanic.Docking), Spawn = SpawnAt(station, Vector3.zero),
                MainCargo = PutCargo(station, module, new Vector3(-1.28f, 0.52f, 3.15f), false, light),
                BonusCargo = PutCargo(station, MakeSpecialCargo("OrbitalFuelCell"), new Vector3(2.78f, 0.61f, 2.84f), true, amber),
                MainLabel = "Модуль питания", BonusLabel = "Резервный аккумулятор",
                Checkpoints = checks,
                MainZone = main, BonusZone = backup
            };
        }

        private static void IndustrialShell(Transform parent, Material floor, Material wall, Material roof, float width,
            float length, float height, bool clerestory = false, bool refrigeration = false)
        {
            Floor(parent, floor, width, -3f, length);
            if (clerestory)
            {
                Partition(parent, wall, -width / 2f, width / 2f, 0f, height, -3f);
                Partition(parent, wall, -width / 2f, width / 2f, 0f, height, length);
                foreach (float x in new[] { -width / 2f, width / 2f })
                {
                    float z = (length - 3f) * 0.5f;
                    Block(parent, "Lower clerestory wall", new Vector3(x, 1.99f, z), new Vector3(0.30f, 3.98f, length + 3f), wall, true);
                    Block(parent, "Clerestory lintel", new Vector3(x, (height + 5.68f) * 0.5f, z),
                        new Vector3(0.42f, height - 5.68f, length + 3f), wall);
                    for (float mullion = -2f; mullion < length; mullion += 2.7f)
                        Block(parent, "Clerestory mullion", new Vector3(x, 4.85f, mullion),
                            new Vector3(0.36f, 1.75f, 0.14f), roof);
                }
            }
            else
            {
                WallBox(parent, wall, width, -3f, length, height);
            }
            Block(parent, "Industrial roof", new Vector3(0, height + 0.17f, (length - 3f) * 0.5f),
                new Vector3(width + 0.26f, 0.34f, length + 3.3f), roof);
            Material frame = Material("Industrial Frame", "#293841");
            Material lamp = refrigeration
                ? Material("Refrigerated Ceiling Light", "#BBE2F0", emissive: true)
                : Material("Ceiling Light", "#F2D7AE", emissive: true);
            Color lightColor = refrigeration ? new Color(0.62f, 0.87f, 1f) : new Color(1f, 0.86f, 0.68f);
            for (float z = 2.0f; z < length; z += 8.9f)
            {
                Block(parent, "Ceiling truss", new Vector3(0f, height - 0.36f, z),
                    new Vector3(width + 0.09f, 0.53f, 0.32f), frame);
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = side * width * 0.46f;
                    Block(parent, "Steel hall column", new Vector3(x, height * 0.50f, z), new Vector3(0.35f, height, 0.42f), frame, true);
                    RoundedBlock(parent, "Ceiling strip light", new Vector3(side * 3.9f, height - 0.68f, z),
                        new Vector3(2.25f, 0.15f, 0.25f), lamp);
                    Lamp(parent, new Vector3(side * 3.7f, height - 0.84f, z), lightColor, 11.7f, 5.15f);
                }
            }
        }

        private static void CheckArch(Transform parent, Vector3 point, float width, Material frame, Material paint, string label)
        {
            foreach (float side in new[] { -1f, 1f })
                Block(parent, "Route arch support", point + new Vector3(side * width * 0.5f, 1.61f, 0f),
                    new Vector3(0.26f, 3.22f, 0.28f), frame, true);
            Block(parent, "Route arch beam", point + new Vector3(0, 3.18f, 0), new Vector3(width + 0.27f, 0.35f, 0.29f), frame, true);
            CourseLabel(parent, label, point + new Vector3(0, 3.20f, -0.17f), width * 0.64f, paint, frame);
        }
    }
}
