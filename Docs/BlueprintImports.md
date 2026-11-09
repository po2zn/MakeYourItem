# 코드창 import

코드창 제목을 Python 모듈 이름처럼 사용합니다. 제목을 더블 클릭해 `smith`로 바꾸고 다음 코드를 작성하세요. 공백 없이 문자 또는 `_`로 시작하는 이름이며 한글도 가능합니다. 제목에는 `.py`를 붙이지 않습니다.

```python
# smith 코드창
def forge(material):
    blade = 1(material)
    2(blade, 7)
    3(blade)
    4(blade, 0)
    return blade
```

다른 코드창에서 실행합니다.

```python
import smith
res = smith.forge(1)
!res
```

다음 Python 표기도 지원합니다.

```python
import smith as s
res = s.forge(1)
!res
```

```python
from smith import forge
res = forge(1)
!res
```

```python
from smith import forge as make
res = make(1)
!res
```

`import a, b as c`, `from a import first, second as other`, `from a import (first, second,)`, `from a import *`도 지원합니다. 별표는 `_`로 시작하지 않는 함수와 변수를 가져옵니다. 모듈 속 변수 조회(`smith.TARGET`), 함수 참조 대입(`make = smith.forge`), 모듈의 다른 모듈 가져오기, 조건문·함수 안의 import도 가능합니다. 함수 정의에는 기존 숫자 표기 `333` / `<<`도 그대로 쓸 수 있습니다.

## 실행 규칙

- 실행 시 열려 있는 코드창의 최신 내용을 복사해 사용합니다. 같은 실행에서 모듈은 최초 import 때 한 번 초기화됩니다. 다음 실행에서는 변경된 내용을 다시 읽습니다.
- 모듈의 최상위 코드는 가져올 때 실행됩니다. 함수 정의만 있는 창은 호출 전까지 제작하지 않습니다. 최상위에 `1(1)`이 있으면 import 시 재료와 비용을 소비합니다.
- 함수는 자신이 정의된 모듈의 변수·함수·import를 참조합니다. 지역 변수는 호출마다 새로 만들며 바깥으로 새지 않습니다. 기존 언어의 함수 사전 등록과 컬렉션 값 복사 규칙은 유지합니다.
- 모든 모듈의 비용, 재료, 난수, 출력, 실행 한도와 공정 기록은 하나의 실행에 합산됩니다. 시험 실행은 실제 자원을 바꾸지 않습니다. 가져온 함수의 공정도 대장장이가 호출 순서대로 수행합니다.
- 모듈에서 발생한 오류와 공정 기록에는 코드창 이름과 그 창의 원본 줄 번호를 표시합니다. 오류 전에 성공한 공정은 기존 규칙대로 남습니다.
- `!` 제출은 실행 버튼을 누른 창에서 합니다. 공유 코드창의 직접 실행 예제는 아래처럼 분리하세요.

```python
if __name__ == "__main__":
    res = forge(1)
    !res
```

직접 실행한 창의 `__name__`은 `"__main__"`, 가져온 창은 해당 모듈 이름입니다.

## 지원 범위

현재 열린 코드창을 대상으로 합니다. 외부 Python 라이브러리·파일 경로·패키지 하위 모듈·상대 import·여러 줄 괄호 import·`__all__` 지정은 지원하지 않습니다. 코드창 이름이 같거나 정규화한 이름이 겹치면 가져오기를 중단하고 제목 변경을 안내합니다. 가져오기 별칭은 게임 내장 함수명과 겹칠 수 없습니다. `import *`는 함수 밖에서만 허용합니다. 직접·상호 재귀 호출은 지원하며 함수 호출 깊이32와 전체 실행10000명령 한도를 공유합니다. 모듈 초기화 중의 순환 import는 별도로 제한하며, 한 실행에서 최대 64개 모듈과 32단계 import를 허용합니다.

문법 및 기본 모듈 동작 참고: [Python 공식 Modules 안내](https://docs.python.org/3/tutorial/modules.html), [import 문법](https://docs.python.org/3/reference/simple_stmts.html#import).

검사: Tools > MakeYourItem > Validate Blueprint Imports.

## 검증 결과

Unity 6000.4.5f1 참조 전체 컴파일, import 21개 검사, 자유 제작 12개 및 기존 언어·출력·변수·재료·경제·안내서 회귀 검사를 통과했습니다. 별도 Unity Play Mode에서 실제 ForgeMvp 코드창 목록을 사용해 가져오기, 별칭 호출, 시험 실행 무소비, 실제 철 1개/12G 소비와 28G 상품 등록, 네 공정 애니메이션, 모듈 수정 재반영, 중복·변경된 제목 처리를 검증했습니다. 로그는 Temp/ImportChecks.txt와 Temp/ImportPlayMode.txt입니다. 테스트 에디터의 검색 인덱스 초기화 예외는 별도로 기록되었으며 제작 코드나 검증 실행에서 발생한 예외는 아닙니다. 헤드리스 검사로 마우스 조작이나 안내서의 시각 배치는 검증하지 않았습니다.
