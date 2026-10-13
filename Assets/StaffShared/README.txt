STAFF 공용 씬 시스템

적용된 씬
- Assets/Scenes/STAFF_Subway_Reference.unity
- Assets/Scenes/SampleScene.unity
- Assets/Scenes/STAFF_City_Reference.unity

공통 원본 (Assets/StaffShared/Prefabs)
1. STAFF Gameplay.prefab
   Characters와 Cameras를 함께 소유합니다. 캐릭터 모델/Animator/InputActions,
   CharacterSwitcher, AdventurePlayer, AdventureCamera 및 기존 1·3인칭,
   춤/표정/캐릭터 선택 등의 런타임 기능을 유지합니다.
   세 씬은 복사본이 아니라 이 프리팹의 연결된 인스턴스입니다.
   코드 변경은 모든 씬에 적용됩니다. 컴포넌트 추가·기본 설정 변경은
   STAFF > Shared > Open Gameplay Prefab에서 공통 원본을 편집하세요.
   씬 인스턴스에서 편집했다면 Overrides > Apply로 공통 원본에 적용해야 합니다.
   Unity의 씬별 override는 의도적으로 자동 전파하지 않습니다.
   시작 위치/회전은 씬 인스턴스 루트의 Transform에서 바꾸세요.
   Scene bindings(GameplaySceneContext)는 씬별 환경/좌표 공간/반사 바닥을
   지정합니다. 공통 프리팹에 다른 씬의 오브젝트 참조를 저장하지 않습니다.

2. STAFF Sky.prefab
   지하철에서 사용하던 Nishita GPU 대기, 시간 프리셋/연속 태양 주기,
   NASA 달 표면/위상/광륜, HYG 별/회전, 체적 구름/바람/시간대 색상,
   태양 플레어와 광선, 열차 램프 광선 설정을 보존합니다.
   Solar directional light는 같은 태양 방향/고도에 맞춰 조명을 갱신합니다.
   구름 Volume Profile과 PC_Renderer.asset의 기존 렌더러 기능을 공유합니다.
   카메라는 이 프로젝트의 공용 PC 렌더러(인덱스 0)를 자동 사용합니다.
   STAFF > Shared > Open Sky Prefab에서 공통 기본값을 편집하세요.
   Nishita time presets 컴포넌트: T 시간대 / F9 플레어 / F10 광선.
   카메라 배열을 씬마다 수동 연결할 필요가 없습니다.
   추가 로드된 씬은 카메라 소속 씬의 하늘을 선택합니다.
   RenderSettings, 태양 조명, 전역 구름 Volume은 활성 씬이 소유합니다.
   동시에 다른 날씨를 가진 여러 씬을 한 카메라로 합성하는 구성은 대상이 아닙니다.

3. STAFF Subway.prefab
   지하철 씬은 5칸, 도시 고가철도는 3칸 인스턴스로 같은 원본을 참조합니다.
   Inspector의 Car Count: 2~8칸. 런타임 API: SubwayConsist.CarCount.
   뒤쪽 Rear + Middle × (칸 수 - 2) + 앞쪽 Front를 생성합니다.
   각 칸은 Subway Rear / Middle / Front.prefab을 공유합니다.
   기존 모델·문 이동 곡선·좌석·실내 바닥·문 충돌체를 원본에서 추출했습니다.
   연결 통로, 문 배열과 이동/탑승 영역은 칸 수에 맞춰 함께 생성됩니다.
   1 이하 입력은 2, 9 이상 입력은 8로 제한합니다.
   칸 수 변경/Rebuild는 열차를 정차 상태로 초기화합니다.
   루트 이동/회전/균일 스케일이 가능합니다. 선로 방향은 루트의 local +Z입니다.
   O 문 열기/닫기, ] 출발, [ 도착. 플레이어 입력이 활성화된 상태에서 작동합니다.
   조립된 Cars는 저장되지 않는 미리보기/실행 오브젝트이며 로드 시 생성됩니다.
   모델/문/충돌체 수정은 생성된 Cars 대신 세 칸 모듈 프리팹에서 하세요.
   역/플랫폼/선로는 씬의 환경으로 남습니다. 이전 5칸에 맞춘 고정 플랫폼
   문턱 차단벽은 제거했습니다. 8칸을 모두 수용하려면 역 길이도 확보하세요.
   동일 씬 여러 열차에서는 O/[ / ] 입력이 모든 열차에 전달되는 기존 동작을
   유지합니다. 개별 열차를 조작하려면 SubwayDoors/SubwayTransit API를 사용하세요.

새 씬
- STAFF > Shared > Install In Current Scene: 공통 캐릭터/카메라와 하늘 설치.
- STAFF Subway.prefab을 원하는 위치에 배치하고 칸 수와 선로 위치를 설정.
- 공용 PC URP renderer의 Clouds / Stars / Solar Optics / Local Godrays
  기능을 끄거나 제거하면 해당 효과도 꺼집니다.
- 기존 도시 재생성 메뉴는 공용 Gameplay/Sky 설치를 유지하도록 연결했습니다.
- 기존 지하철의 단계별 원본 제작 도구는 당시 씬 구조를 전제로 합니다.
  공용화 이후에는 프리팹을 편집하고, 원본 제작 도구로 씬을 덮어쓰지 마세요.

검증
- 공통 Gameplay 원본에 임시 컴포넌트 추가 → 세 씬 전파 확인 → 검사 후 제거.
- 세 씬 모두 연결된 Gameplay/Sky, 활성 카메라 1개, 모델·하늘 참조 확인.
- 2,3,4,5,6,7,8칸 문 곡선/게이트 연결 및 열고 닫기 검사.
- Play 모드에서 2~8칸 생성, 이동·회전한 8칸 열차 탑승자 운반/출발/도착 검사.
- 세 씬 실제 Play 실행, 낮/밤 렌더, 연속 태양 주기 검사.
- 실제 검수 이미지/결과: docs/SharedSystems.
- 이전 씬 백업: Library/StaffShared/Backups (Unity Library는 Git에 저장되지 않음).
