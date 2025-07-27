package com.avithon.neurocontrolapp;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.core.type.TypeReference;
import javafx.application.Platform;
import javafx.beans.property.StringProperty;
import javafx.fxml.FXML;
import javafx.fxml.Initializable;
import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.geometry.Side;
import javafx.scene.chart.LineChart;
import javafx.scene.chart.NumberAxis;
import javafx.scene.chart.XYChart;
import javafx.scene.control.Label;
import javafx.scene.layout.VBox;
import javafx.scene.text.Font;
import javafx.scene.text.FontWeight;

import java.net.URL;
import java.time.*;
import java.util.HashMap;
import java.util.Map;
import java.util.ResourceBundle;
import java.util.logging.Level;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class PopupController implements Initializable {

    @FXML
    private LineChart<Number, Number> rawEeg;
    @FXML
    private LineChart<Number, Number> alpha;
    @FXML
    private LineChart<Number, Number> beta;
    @FXML
    private LineChart<Number, Number> gamma;
    @FXML
    private LineChart<Number, Number> theta;
    @FXML
    private LineChart<Number, Number> delta;
    @FXML
    private LineChart<Number, Number> attentionMediation;
    @FXML
    private VBox vbAtco;

    private Cwp cwp;

    public Thread thread;
    private int eegInterval = 100;
    private int interval = 200;
    long baseTimeStamp = -1;

    @Override
    public void initialize(URL url, ResourceBundle resourceBundle) {

        setLineCharPropertiesToRepresentWaves(rawEeg, -2500, 2500);
        rawEeg.getYAxis().setAutoRanging(false);
        ((NumberAxis)rawEeg.getYAxis()).setUpperBound(250);
        ((NumberAxis)rawEeg.getYAxis()).setLowerBound(-250);
        setLineCharPropertiesToRepresentWaves(alpha, 0, 256);
        setLineCharPropertiesToRepresentWaves(beta, 0, 256);

        setLineCharPropertiesToRepresentWaves(gamma, 0, 256);
        setLineCharPropertiesToRepresentWaves(theta, 0, 256);
        setLineCharPropertiesToRepresentWaves(delta, 0, 256);
        setLineCharPropertiesToRepresentWaves(attentionMediation, 0, 256);

    }



    private void changeChartValue(LineChart<Number, Number> lineChart, Integer power, double millis, int interval) {
        Platform.runLater(()->{
            if (lineChart.getData().isEmpty()){
                XYChart.Series<Number, Number> series1 = new XYChart.Series<>();
                series1.setName("POWER");
                series1.getData().add(new XYChart.Data<>(millis, power));
                lineChart.getData().add(series1);
            }else {
                lineChart.getData().getFirst().getData().add(new XYChart.Data<>(millis, power));
            }
            if (millis <interval) {
                ((NumberAxis) lineChart.getXAxis()).setLowerBound(0);
                ((NumberAxis) lineChart.getXAxis()).setUpperBound(interval);
            }else {
                ((NumberAxis) lineChart.getXAxis()).setLowerBound(millis -interval);
                ((NumberAxis) lineChart.getXAxis()).setUpperBound(millis);
            }

            for (XYChart.Series<Number, Number> series : lineChart.getData()) {
                series.getData().removeIf(d->d.getXValue().doubleValue()<millis - interval);
            }
        });
    }

    private void changeChartValue(LineChart<Number, Number> lineChart, Integer low, Integer high, double millis, int interval) {
        Platform.runLater(()->{
            if (lineChart.getData().isEmpty()){
                XYChart.Series<Number, Number> series1 = new XYChart.Series<>();
                series1.setName("LOW");
                series1.getData().add(new XYChart.Data<>(millis, low));
                lineChart.getData().add(series1);
            }else {
                lineChart.getData().getFirst().getData().add(new XYChart.Data<>(millis, low));
            }
            if (millis<interval) {
                ((NumberAxis) lineChart.getXAxis()).setLowerBound(0);
                ((NumberAxis) lineChart.getXAxis()).setUpperBound(interval);// show only last 100 units
            }else {
                ((NumberAxis) lineChart.getXAxis()).setLowerBound(millis -interval);
                ((NumberAxis) lineChart.getXAxis()).setUpperBound(millis);// show only last 100 units
            }


                if (lineChart.getData().size()<2){
                    XYChart.Series<Number, Number> series1 = new XYChart.Series<>();
                    series1.setName("HIGH");
                    series1.getData().add(new XYChart.Data<>(millis, high));
                    lineChart.getData().add(series1);
                }else {
                    lineChart.getData().getLast().getData().add(new XYChart.Data<>(millis, high));
                }


            for (XYChart.Series<Number, Number> series : lineChart.getData()) {
                series.getData().removeIf(d->d.getXValue().doubleValue()<millis - interval);

            }
        });

    }



    private void setLineCharPropertiesToRepresentWaves(LineChart<Number, Number> lineChart, double lowerBoundY, double upperBoundY) {
        lineChart.setCreateSymbols(false);
        lineChart.setAnimated(false);
        lineChart.getXAxis().setAutoRanging(false); // You control the range
        ((NumberAxis)lineChart.getXAxis()).setLowerBound(0);
        ((NumberAxis)lineChart.getXAxis()).setUpperBound(interval);// show only last 100 units
        lineChart.getXAxis().setTickLabelsVisible(false);
        lineChart.setLegendSide(Side.RIGHT);
        lineChart.setLegendVisible(true);

//        lineChart.getYAxis().setAutoRanging(true); // You control the range


    }

    public void addCwp(Cwp clone) {
        this.cwp = clone;
        vbAtco.getChildren().add(clone.vbMain);
        for (Map.Entry<String,StringProperty> val: cwp.states.entrySet()){

            VBox vb = new VBox();
            vb.setSpacing(5);
            vb.setAlignment(Pos.CENTER);
            vb.setStyle(
                    "-fx-border-color:  lightgray; " +
                            "-fx-border-width: 1px; " +
                            "-fx-border-radius: 12px; " +
                            "-fx-background-radius: 12px; " +
                            "-fx-background-color: white; " +
                            "-fx-padding: 10px 10px 10px 10px; ");
            vb.setPadding(new Insets(10,0,10,0));
            Label label = new Label(val.getValue().get());
//            label.setFont(Font.font("Segoe UI", FontWeight.BOLD, 12));
            label.textProperty().bind(val.getValue());

            Label lblValue = new Label(cwp.statesValues.get(val.getKey()).get());
            lblValue.setFont(Font.font("Segoe UI", FontWeight.BOLD, 12));

            lblValue.textProperty().bind(cwp.statesValues.get(val.getKey()));
            ;
            vb.getChildren().addAll(label,lblValue);
            vbAtco.getChildren().add(vb);
        }
//        vbAtco.getChildren().add(clone.vbMain);
//        vbAtco.getChildren().add(clone.vbMain);
        startThread();
    }

    private void startThread() {
        thread = new Thread(()->{
            System.out.println("Thread: " + Thread.currentThread().getName());

            while (!thread.isInterrupted()){
                HashMap<String, Object> map = null;
                try {
                    String json = HelloApplication.queues.get(cwp.id).poll();
                    if (json != null && !json.isBlank()) {
                        map = HelloApplication.objectMapper.readValue(json, new TypeReference<HashMap<String, Object>>() {
                        });

                    }
                } catch (JsonProcessingException ex) {
                    HelloApplication.LOGGER.log(Level.SEVERE,ex.getMessage());

                }
                if (map==null){
                    continue;
                }
                Integer lowGammaValue = (Integer) map.get("LowGamma");
                Integer highGammaValue = (Integer) map.get("HighGamm");

                Integer lowBetaValue = (Integer) map.get("LowBeta");
                Integer highBetaValue = (Integer) map.get("HighBeta");

                Integer lowAlphaValue = (Integer) map.get("LowAlpha");
                Integer highAlphaValue = (Integer) map.get("HighAlpha");

                Integer rawEEGValue = (Integer) map.get("RawEEG");

                Integer meditationValue = (Integer) map.get("Meditation");
                Integer attentionValue = (Integer) map.get("Attention");



                String timeValue = map.get("Time").toString();


                Integer thetaValue = (Integer) map.get("Theta");

                Integer deltaValue = (Integer) map.get("Delta");


//                LocalDateTime localDateTime = LocalDateTime.parse(timeValue, DateTimeFormatter.ISO_LOCAL_DATE_TIME);
                LocalDateTime localDateTime = LocalDateTime.ofInstant(parseToUTC(timeValue), ZoneId.systemDefault());
                long timestamp  = localDateTime.atZone(ZoneId.systemDefault()).toInstant().toEpochMilli();
                if (baseTimeStamp == -1)
                    baseTimeStamp = timestamp;

                double timeInSeconds = (timestamp - baseTimeStamp) / 1000.0;

                if (lowGammaValue!=0){
                    changeChartValue(gamma,lowGammaValue, highGammaValue, timeInSeconds, interval);
                }
                if (lowBetaValue!=0){
                    changeChartValue(beta, lowBetaValue, highBetaValue, timeInSeconds, interval);
                }
                if (lowAlphaValue!=0){
                    changeChartValue(alpha,lowAlphaValue, highAlphaValue, timeInSeconds, interval);
                }
                if (meditationValue!=0){
                    changeChartValue(attentionMediation,meditationValue, attentionValue, timeInSeconds, interval);
                }
                if (rawEEGValue!=0){
                    changeChartValue(rawEeg,rawEEGValue, timeInSeconds, eegInterval);
                }
                if (thetaValue!=0){
                    changeChartValue(theta,thetaValue, timeInSeconds, interval);
                }
                if (deltaValue!=0){
                    changeChartValue(delta,deltaValue, timeInSeconds, interval);
                }

            }
            System.out.println("CWP thread: INTERRUPTED");
        });
        thread.setName("CWP Thread");
        thread.start();
    }

    public static Instant parseToUTC(String input) {
        // Step 1: Normalize fractional seconds to max 9 digits (nanoseconds)
        String fixed = normalizeFraction(input);

        // Step 2: Check if input has offset/timezone
        if (fixed.matches(".*[+-]\\d{2}:\\d{2}$")) {
            // Parse as OffsetDateTime and convert to UTC
            OffsetDateTime odt = OffsetDateTime.parse(fixed);
            return odt.toInstant();  // UTC
        } else {
            // No offset: assume it's local time and treat it as in system default zone
            LocalDateTime ldt = LocalDateTime.parse(fixed);
            ZonedDateTime zdt = ldt.atZone(ZoneId.systemDefault()); // or use ZoneId.of("Europe/...")
            return zdt.withZoneSameInstant(ZoneOffset.UTC).toInstant();
        }
    }

    private static String normalizeFraction(String input) {
        Pattern pattern = Pattern.compile("(\\.\\d{1,9})\\d*(?=[+-]|$)");
        Matcher matcher = pattern.matcher(input);
        if (matcher.find()) {
            String fraction = matcher.group(1);
            return matcher.replaceFirst(fraction);
        }
        return input;
    }

}
