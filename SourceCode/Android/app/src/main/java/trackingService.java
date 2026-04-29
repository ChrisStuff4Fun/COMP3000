import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.location.Location;
import android.os.IBinder;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import com.example.comp3000androidapp.Crypto;
import com.example.comp3000androidapp.apiManager;
import com.google.android.gms.location.LocationCallback;
import com.google.android.gms.location.LocationResult;

public class trackingService extends Service {

    private LocationCallback callback;
    private locationManager locationManager;
    private String cachedBfvKey = null;
    private final Crypto crypto = new Crypto();
    private final apiManager api = new apiManager();

    public void onCreate() {
        super.onCreate();

        // fetch BFV key once on service start, cache it
        api.fetchServerBfvKey(new apiManager.bfvKeyCallback() {
            @Override
            public void onSuccess(String key) {
                cachedBfvKey = key;
            }
            @Override
            public void onError(Exception e) {
                e.printStackTrace();
                // service will skip sending until key is available
            }
        });

        callback = new LocationCallback() {
            @Override
            public void onLocationResult(@NonNull LocationResult result) {
                Location location = result.getLastLocation();
                if (location == null) return;
                if (cachedBfvKey == null) return; // key not ready yet, skip

                SharedPreferences prefs = getApplicationContext()
                        .getSharedPreferences("cybertrackClient", Context.MODE_PRIVATE);
                String deviceId = prefs.getString("device_id", null);

                // encrypt with SEAL on background thread (crypto is heavy)
                new Thread(() -> {
                    String[] encrypted = crypto.encryptLocation(
                            cachedBfvKey,
                            location.getLatitude(),
                            location.getLongitude()
                    );
                    api.sendLocation(deviceId, encrypted[0], encrypted[1]);
                }).start();
            }
        };

        locationManager = new locationManager(this, callback);
        locationManager.startLocationUpdates();
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        return START_STICKY;
    }

    @Nullable
    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }
}