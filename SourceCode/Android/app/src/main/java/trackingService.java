import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.location.Location;
import android.os.IBinder;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import com.google.android.gms.location.LocationCallback;
import com.google.android.gms.location.LocationResult;
import com.example.comp3000androidapp.encryptionManager;
import com.example.comp3000androidapp.apiManager;

public class trackingService extends Service {

    private LocationCallback callback;
    private locationManager locationManager;



    // onCreate
    // Service initialiser, sets the callback process and starts location updates
    public void onCreate() {
        super.onCreate();

        callback = new LocationCallback() { // Define callback for when location is given
            @Override
            public void onLocationResult(@NonNull LocationResult result) {

                Location location = result.getLastLocation();

                // Stop here if location failed (possible but unlikely)
                if (location == null) return;

                // instantiate managers
                encryptionManager crypto = encryptionManager.getInstance();
                apiManager api = new apiManager();

                encryptionManager.EncryptedLocation enc = crypto.encryptLocation(location.getLatitude(), location.getLongitude());

                Context context = getApplicationContext();

                SharedPreferences prefs =
                        context.getSharedPreferences("cybertrackClient", Context.MODE_PRIVATE);

                String deviceId = prefs.getString("device_id", null);


                api.sendLocation(deviceId, enc.lat, enc.lon);
            }
        };

        // Initialise a locationManager class and begin updates
        locationManager = new locationManager(this, callback);
        locationManager.startLocationUpdates();

    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        // Tell Android to restart the service if it gets killed
        return START_STICKY;
    }

    @Nullable
    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

}
