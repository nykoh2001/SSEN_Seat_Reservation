# SSEN 스터디카페 좌석 예약

WPF 기반 스터디카페 좌석 예약 데스크톱 앱입니다. View, ViewModel, Model, Service의 책임을 나눈 MVVM 구조로 작성되어 있습니다.

## 전체 아키텍처

```text
+----------------------------- VIEW --------------------------+
| MainWindow.xaml                                             |
|  Layout, 좌석 표시, 상태 텍스트, 입력 상자와 버튼            |
+------------------------------+------------------------------+
                               |
                  DataContext / 바인딩 / ICommand
                               v
+-------------------------- VIEWMODEL ------------------------+
| MainViewModel                                               |
|  화면 상태, 선택 좌석, 명령, 타이머, 좌석 표시 목록           |
+------------------+----------------------+------------------+
                   |                      |
        Seat 상태  |                      | 저장 / 불러오기
          변경     v                      v
+------------------------+   +-------------------------------+
| MODEL                  |   | SERVICE                       |
| Seat                   |   | SeatStorageService            |
| 좌석 데이터와 상태      |   | seats.json 읽기 / 쓰기         |
| PropertyChanged 알림   |   +-------------------------------+
+-----------+------------+
            | 속성 변경 알림
            +-------------------------------------> VIEW 갱신

+------------------------------+
| MessageBoxConfirmationService|
| 예약 취소 확인 창             |
+---------------+--------------+
                | 확인 요청
                +-------------------------------> MainViewModel
```

## MVVM 구성 요소

| 역할 | 파일 | 책임 |
| --- | --- | --- |
| View | `MainWindow.xaml` | 화면 배치, 스타일, 데이터와 명령의 바인딩 선언 |
| View 연결 | `MainWindow.xaml.cs` | `MainViewModel`을 생성해 `DataContext`로 설정하고 창 종료 시 정리 |
| ViewModel | `ViewModels/MainViewModel.cs` | 화면 상태, 좌석 선택, 예약 명령, 타이머와 화면에 표시할 값 관리 |
| 명령 | `ViewModels/RelayCommand.cs` | ViewModel의 동작을 WPF `ICommand`로 제공 |
| ViewModel 기반 | `ViewModels/ViewModelBase.cs` | 속성 변경을 알리는 `INotifyPropertyChanged` 구현 기반 |
| Model | `Seat.cs` | 좌석 번호, 상태, 이용자, 시간 및 상태 변경 알림 |
| 저장 서비스 | `Services/SeatStorageService.cs` | `seats.json`에서 좌석을 읽고 저장 |
| 확인 서비스 | `Services/IConfirmationService.cs`, `Services/MessageBoxConfirmationService.cs` | 취소 확인 기능의 추상화와 WPF 확인 창 표시 |

## WPF 바인딩 이해하기

### DataContext: 바인딩이 값을 찾는 기준

`MainWindow.xaml.cs`는 창을 만들 때 ViewModel을 `DataContext`에 지정합니다.

```csharp
viewModel = new MainViewModel();
DataContext = viewModel;
```

XAML의 `{Binding ...}`은 기본적으로 현재 컨트롤의 `DataContext`에서 해당 속성을 찾습니다. 창의 `DataContext`는 자식 컨트롤로 상속되므로, 화면의 일반적인 바인딩은 `MainViewModel`의 속성을 가리킵니다.

### 좌석 목록: ItemsSource와 DataTemplate

좌석 배치의 `ItemsControl`은 ViewModel의 구역별 좌석 컬렉션을 표시합니다.

```xml
<ItemsControl ItemsSource="{Binding Row1Section1}"
              ItemTemplate="{StaticResource SeatTemplate}" />
```

- `ItemsSource`는 화면에 반복해서 표시할 목록입니다.
- `ItemTemplate`은 목록의 항목 하나를 어떻게 그릴지 정합니다.
- `MainViewModel`은 31개 좌석을 `2 / 2 / 3`석 구역과 `0 / 2 / 3`석 구역으로 나눠 제공합니다.

`ItemsControl`이 각 좌석을 템플릿으로 표시할 때, 템플릿 내부의 `DataContext`는 ViewModel이 아니라 **현재 항목인 `Seat` 객체**가 됩니다. 따라서 좌석 템플릿의 다음 바인딩은 각각 `Seat.Number`, `Seat.StatusText`, `Seat.UserName`, `Seat.TimeText`를 읽습니다.

```xml
<TextBlock Text="{Binding Number}" />
<TextBlock Text="{Binding StatusText}" />
```

### 좌석 선택: 템플릿에서 ViewModel 명령 찾기

좌석 버튼 안의 현재 `DataContext`는 `Seat`입니다. 선택 명령은 ViewModel에 있으므로, 버튼은 조상인 `Window`를 찾아 그 `DataContext`에서 명령을 가져옵니다.

```xml
Command="{Binding DataContext.SelectSeatCommand,
                  RelativeSource={RelativeSource AncestorType={x:Type Window}}}"
CommandParameter="{Binding}"
```

`CommandParameter`의 `{Binding}`은 현재 템플릿 항목, 즉 선택된 `Seat` 객체입니다. ViewModel은 전달받은 좌석을 `SelectedSeat`로 설정합니다.

### 속성 바인딩: 화면 값 자동 갱신

예를 들어 좌석 상태가 변경되면 `Seat`가 `PropertyChanged` 알림을 발생시킵니다. WPF는 변경 알림을 받은 속성에 연결된 텍스트와 스타일을 다시 읽습니다. 좌석의 색상은 `Status`를 확인하는 `DataTrigger`로 바뀝니다.

ViewModel도 `ViewModelBase`를 통해 `INotifyPropertyChanged`를 구현합니다. 선택된 좌석이나 좌석 수처럼 ViewModel에 있는 표시 값이 바뀌면 같은 방식으로 해당 화면이 갱신됩니다.

### 입력 바인딩: TextBox와 TwoWay

이름 입력은 다음처럼 ViewModel의 `Name` 속성에 연결됩니다.

```xml
<TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" />
```

`TextBox.Text`는 기본적으로 **TwoWay** 바인딩입니다. ViewModel 값은 입력 상자에 표시되고, 사용자가 입력한 값은 다시 ViewModel로 전달됩니다. `UpdateSourceTrigger=PropertyChanged`는 입력이 바뀔 때마다 즉시 `Name`을 갱신합니다.

화면에 출력만 하는 `TextBlock.Text` 같은 바인딩은 기본적으로 **OneWay**입니다. ViewModel이나 Model에서 값이 바뀌면 화면에 반영되지만, 화면에서 값을 입력해 원본 속성을 바꾸지는 않습니다.

### 버튼 동작: ICommand

예약, 입실, 취소, 퇴실 버튼은 클릭 이벤트 대신 ViewModel의 명령에 연결됩니다.

```xml
<Button Content="예약하기" Command="{Binding ReserveCommand}" />
```

`RelayCommand`는 `ICommand`를 구현해 ViewModel의 동작을 버튼에서 실행할 수 있게 합니다. 명령은 실행 가능 조건도 제공하므로, 좌석 미선택 또는 이름 미입력 등의 상태에 따라 WPF가 버튼을 비활성화할 수 있습니다.

## 동작 예: 좌석 예약

1. 좌석 버튼의 `SelectSeatCommand`가 현재 `Seat`를 ViewModel에 전달합니다.
2. 사용자가 이름을 입력하면 TwoWay 바인딩이 `MainViewModel.Name`을 갱신합니다.
3. 예약 버튼이 `ReserveCommand`를 실행합니다.
4. ViewModel이 입력을 확인하고 `Seat.Reserve()`를 호출합니다.
5. `Seat`가 상태 변경을 알리면 좌석 텍스트, 색상, 좌석 수가 갱신됩니다.
6. ViewModel이 `SeatStorageService`를 호출해 변경된 좌석 상태를 저장합니다.

예약 취소 확인 창은 `IConfirmationService` 추상화를 통해 표시합니다. ViewModel은 확인 기능의 인터페이스에만 의존하고, WPF `MessageBox` 호출은 구현 서비스가 담당합니다.
