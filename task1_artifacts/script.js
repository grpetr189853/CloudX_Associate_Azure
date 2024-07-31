import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
  vus: 10,
  duration: '30s',
  cloud: {
    // Project: Default project
    projectID: 3707231,
    // Test runs with the same name groups test runs together.
    name: 'Test (30/07/2024-18:59:47)'
  }
};

export default function() {
  http.get('http://webtrafficmanager.trafficmanager.net');
  sleep(1);
}