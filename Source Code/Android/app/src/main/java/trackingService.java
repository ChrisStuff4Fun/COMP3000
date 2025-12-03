import android.app.Service;
import android.content.Intent;
import android.location.Location;
import android.os.IBinder;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import com.google.android.gms.location.LocationCallback;
import com.google.android.gms.location.LocationResult;

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

                double latitude  = location.getLatitude();
                double longitude = location.getLongitude();
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
