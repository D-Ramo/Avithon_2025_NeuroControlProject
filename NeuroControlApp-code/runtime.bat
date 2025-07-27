  jlink ^
    --module-path "%JAVA_HOME%\jmods;input\lib" ^
    --add-modules javafx.base,javafx.controls,javafx.fxml,java.desktop,java.logging,javafx.graphics,javafx.media ^
    --output custom-runtime ^
    --compress=2 ^
    --strip-debug ^
    --no-header-files ^
    --no-man-pages