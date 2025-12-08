import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.os.Looper;
import androidx.core.app.ActivityCompat;
import com.google.android.gms.location.FusedLocationProviderClient;
import com.google.android.gms.location.LocationCallback;
import com.google.android.gms.location.LocationRequest;
import com.google.android.gms.location.LocationServices;
import com.google.android.gms.location.Priority;

public class locationManager {
    private final FusedLocationProviderClient fusedClient;
    private final LocationCallback callback;
    private final Context context;

    public locationManager (Context context, LocationCallback callback) {
        this.fusedClient = LocationServices.getFusedLocationProviderClient(context);
        this.callback    = callback;
        this.context     = context;
    }

    // startLocationUpdates
    // Called by trackingService in order to begin receiving gps data
    public void startLocationUpdates() {

        // Check permissions before generating some strange errors
        if (ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED &&
                ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            return;
        }

        LocationRequest request = new LocationRequest.Builder(Priority.PRIORITY_HIGH_ACCURACY) // Prioritise accuracy, uses all forms of location gathering
                .setMinUpdateDistanceMeters(10) // Will only update if the device has moved > 10m
                .setIntervalMillis(30000) // Calls an update every 30s
                .setMinUpdateIntervalMillis(15000) // Will wait max 15s for an update (to save power)
                .setMaxUpdateDelayMillis(60000) // Will wait 60s at the very most (low power mode etc)
                .build();

        fusedClient.requestLocationUpdates(request, callback, Looper.getMainLooper());
    }





    // removeUpdates
    // Used to kill the loop of location updates
    public void removeUpdates() {
        fusedClient.removeLocationUpdates(callback);
    }


}
