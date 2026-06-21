# Runtime UI Font Setup

자동 생성 UI는 `TextMeshProUGUI`를 사용합니다. 기본 LiberationSans SDF 폰트에는 한글 글리프가 없어 한글이 네모로 보일 수 있습니다.

한글 폰트 연결 방법:

1. 한글을 지원하는 `.ttf` 또는 `.otf` 폰트를 `Assets/Fonts/` 같은 폴더에 넣습니다.
2. Unity에서 폰트 파일을 선택한 뒤 TMP Font Asset을 생성합니다.
   - 예: 우클릭 메뉴 또는 `Window > TextMeshPro > Font Asset Creator`
   - 프로토타입 단계에서는 Atlas Population Mode를 Dynamic으로 두면 편합니다.
3. 씬에 `GameRoot` 오브젝트를 만들고 `GameManager`, `UIManager`를 붙입니다.
4. `UIManager`의 `Runtime UI Font > Korean Tmp Font` 슬롯에 생성한 TMP Font Asset을 연결합니다.
5. Play를 누르면 자동 생성되는 모든 `TextMeshProUGUI`에 이 폰트가 적용됩니다.

폰트를 연결하지 않으면 게임은 계속 실행되지만, Console에 한 번만 경고가 출력되고 한글이 깨질 수 있습니다.
